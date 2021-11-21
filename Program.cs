using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Downloader;
using ShellProgressBar;

namespace cgamos
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            Console.WriteLine($"cgamos-downloader v.{version}");

            await Parser.Default.ParseArguments<Options>(args)
                   .WithParsedAsync<Options>(async options =>
                   {
                       ArchiveRecord record;

                       bool silent = false;
                       if (string.IsNullOrEmpty(options.Fond) ||
                            string.IsNullOrEmpty(options.Opis) ||
                            string.IsNullOrEmpty(options.Delo))
                       {
                           record = ConsoleInputParser.ParseInput();
                       }
                       else
                       {
                           silent = true;
                           record = new ArchiveRecord(options.Fond, options.Opis, options.Delo, Math.Max((short)1, options.Start), options.End);
                       }

                       await Start(options, record, silent);

                       Console.WriteLine($"Завершено за {sw.Elapsed}");
                       if (!silent)
                       {
                           Console.ReadKey();
                       }
                   });
        }

        private static async Task Start(Options options, ArchiveRecord record, bool silent)
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

            Console.WriteLine("Поиск дела...");

            var pageData = await DataParser.GetPageData(record);
            if (pageData == null)
            {
                Console.WriteLine($"Ошибка! Неправильные параметры Фонд #{record.Fond} Опись #{record.Opis} Дело #{record.Delo}");
                if (!silent)
                {
                    Console.ReadKey();
                }

                return;
            }

            if (!record.End.HasValue)
            {
                record = record with { End = pageData.PageCount };
            }

            if (record.End > pageData.PageCount)
            {
                record = record with { End = pageData.PageCount };
            }

            var realPageCount = record.End.Value - record.Start + 1;

            var directoryName = string.IsNullOrEmpty(options.Path)
                 ? $"{record.Fond}_{record.Opis}_{record.Delo}"
                 : $"{options.Path}/{record.Fond}_{record.Opis}_{record.Delo}";

            if (!Directory.Exists(directoryName))
            {
                try
                {
                    Directory.CreateDirectory(directoryName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception while creading directory {directoryName}; Message={ex.Message}");
                    if (!silent)
                    {
                        Console.ReadKey();
                    }

                    return;
                }
            }

            var progressBarString = "{0}/{1} Фонд #{2} Опись #{3} Дело #{4}";
            var progressBarStringFormated = string.Format(progressBarString, record.Start, record.End, record.Fond, record.Opis, record.Delo);

            using (var mainProgressBar = new ProgressBar(realPageCount, progressBarStringFormated, progressBarOptions))
            {
                for (int i = record.Start; i <= record.End; i++)
                {
                    var url = $"https://cgamos.ru{pageData.PageUrls[i - 1]}";

                    var pbar = mainProgressBar.Spawn(100, $"Скачивание {url}", progressBarOptions);

                    EventHandler<Downloader.DownloadProgressChangedEventArgs> handler = (sender, e) =>
                    {
                        var persents = (int)Math.Round(e.ProgressPercentage, 0);
                        pbar.Tick(persents);
                    };

                    downloader.DownloadProgressChanged += handler;

                    try
                    {
                        var path = $"{directoryName}/{GetFileName(url)}";

                        await downloader.DownloadFileTaskAsync(url, path);

                        progressBarStringFormated = string.Format(progressBarString, i, record.End, record.Fond, record.Opis, record.Delo);
                        mainProgressBar.Tick(realPageCount - (record.End.Value - i), progressBarStringFormated);
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

        private static string GetFileName(string url)
        {
            var index = url.LastIndexOf('/');

            return url.Substring(index);
        }
    }

    internal record ArchiveRecord(string Fond, string Opis, string Delo, short Start = 1, short? End = null);

    internal record PageData(string[] PageUrls, short PageCount);
}