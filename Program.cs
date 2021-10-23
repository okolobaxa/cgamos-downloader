using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CommandLine;
using Downloader;
using ShellProgressBar;

namespace cgamos
{
    class Program
    {
        public class Options
        {
            [Option('f', "fond", Required = false, HelpText = "Fond")]
            public string Fond { get; set; }

            [Option('o', "opis", Required = false, HelpText = "Opis")]
            public string Opis { get; set; }

            [Option('d', "delo", Required = false, HelpText = "Delo")]
            public string Delo { get; set; }

            [Option('p', "path", Required = false, HelpText = "Path")]
            public string Path { get; set; }
        }

        static async Task Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            await Parser.Default.ParseArguments<Options>(args)
                   .WithParsedAsync<Options>(async options =>
                   {
                       ArchiveRecord record;

                       if (string.IsNullOrEmpty(options.Fond) ||
                            string.IsNullOrEmpty(options.Opis) ||
                            string.IsNullOrEmpty(options.Delo))
                       {
                           record = ParseInput();
                       }
                       else
                       {
                           record = new ArchiveRecord(options.Fond, options.Opis, options.Delo);
                       }

                       await Start(options, record);

                       Console.WriteLine($"Finished in {sw.Elapsed}");
                       Console.ReadKey();
                   });
        }

        private static async Task Start(Options options, ArchiveRecord record)
        {
            var downloader = new DownloadService(new DownloadConfiguration()
            {
                ChunkCount = 1,
                MaxTryAgainOnFailover = 2,
                ParallelDownload = false,
                RequestConfiguration =
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    KeepAlive = false,
                }
            });

            var progressBarOptions = new ProgressBarOptions
            {
                ProgressCharacter = '=',
                ProgressBarOnBottom = true,
                CollapseWhenFinished = true
            };

            int currentPage = 1;
            var maxPageCount = await GetPageCount(record);
            bool loadNext = true;

            var directoryName = string.IsNullOrEmpty(options.Path)
                 ? $"{Guid.NewGuid()}_{record.Fond}_{record.Opis}_{record.Delo}"
                 : $"{options.Path}/{Guid.NewGuid()}_{record.Fond}_{record.Opis}_{record.Delo}";
            try
            {
                Directory.CreateDirectory(directoryName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while creading directory {directoryName}; Message={ex.Message}");
                Console.ReadKey();

                return;
            }

            var progressBarString = maxPageCount > 0
                ? "{0}/{1} Fond #{2} Opis #{3} Delo #{4}"
                : "Fond #{2} Opis #{3} Delo #{4}";

            using (var mainProgressBar = new ProgressBar(maxPageCount, string.Format(progressBarString, currentPage, maxPageCount, record.Fond, record.Opis, record.Delo), progressBarOptions))
            {
                while (loadNext)
                {
                    string currentPageToken = currentPage.ToString().PadLeft(8, '0');

                    var url = $"https://cgamos.ru/images/archive/01-{record.Fond.PadLeft(4, '0')}-{record.Opis.PadLeft(4, '0')}-{record.Delo.PadLeft(6, '0')}/{currentPageToken}.jpg";
                    var path = $"{directoryName}/{currentPageToken}.jpg";

                    var pbar = mainProgressBar.Spawn(100, $"Downloading {url}", progressBarOptions);

                    EventHandler<Downloader.DownloadProgressChangedEventArgs> handler = (sender, e) =>
                    {
                        var persents = (int)Math.Round(e.ProgressPercentage, 0);
                        pbar.Tick(persents);
                    };

                    downloader.DownloadProgressChanged += handler;

                    try
                    {
                        await downloader.DownloadFileTaskAsync(url, path);

                        var progressBarStringFormated = string.Format(progressBarString, currentPage, maxPageCount, record.Fond, record.Opis, record.Delo);

                        mainProgressBar.Tick(progressBarStringFormated);
                        currentPage++;

                        if (maxPageCount > 0 && currentPage > maxPageCount)
                        {
                            loadNext = false;
                            break;
                        }
                    }
                    catch (WebException)
                    {
                        loadNext = false;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception while loading url={url}; Message={ex.Message}");
                    }
                    finally
                    {
                        downloader.DownloadProgressChanged -= handler;
                        pbar.Dispose();
                    }
                }
            }
        }

        private static async ValueTask<int> GetPageCount(ArchiveRecord record)
        {
            try
            {
                var client = new HttpClient();
                string url = $"https://cgamos.ru/metric-books/{record.Fond}/{record.Fond}-{record.Opis}/{record.Fond}_{record.Opis}_{record.Delo}/";
                var body = await client.GetStringAsync(url);

                //<input type="text" class="input-pages" data-max="216 " value="1">
                var index = body.IndexOf("data-max=\"");
                if (index == -1)
                {
                    Console.WriteLine($"Unable to get total number of pages for url={url}");
                    return 0;
                }

                var start = index + "data-max=\"".Length;
                var end = body.IndexOf(" \"", start);

                var str = body[start..end];

                if (int.TryParse(str.Trim(), out var number))
                {
                    return number;
                }

                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static ArchiveRecord ParseInput()
        {
            string fond = null;
            string opis = null;
            string delo = null;

            while (string.IsNullOrEmpty(fond))
            {
                Console.Write("Fond #: ");
                fond = Console.ReadLine();
                if (string.IsNullOrEmpty(fond))
                {
                    Console.WriteLine("Invalid Fond #");
                }
            }

            while (string.IsNullOrEmpty(opis))
            {
                Console.Write("Opis #: ");
                opis = Console.ReadLine();
                if (string.IsNullOrEmpty(opis))
                {
                    Console.WriteLine("Invalid Opis #");
                }
            }

            while (string.IsNullOrEmpty(delo))
            {
                Console.Write("Delo #: ");
                delo = Console.ReadLine();
                if (string.IsNullOrEmpty(delo))
                {
                    Console.WriteLine("Invalid Delo #");
                }
            }

            return new ArchiveRecord(fond, opis, delo);
        }
    }

    internal record ArchiveRecord(string Fond, string Opis, string Delo);
}
