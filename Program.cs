using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

                       var totalDownloadedSizeInBytes = await Download(options, record, silent);

                       Console.WriteLine($"Завершено за {sw.Elapsed}; Скачано { totalDownloadedSizeInBytes / 1024 / 1024} MB");
                       if (!silent)
                       {
                           Console.ReadKey();
                       }
                   });
        }

        private static async Task<long> Download(Options options, ArchiveRecord record, bool silent)
        {
            Console.WriteLine("Поиск дела...");

            var pageData = await DataParser.GetPageData(record);
            if (pageData == null)
            {
                Console.WriteLine($"Ошибка! Неправильные параметры Фонд #{record.Fond} Опись #{record.Opis} Дело #{record.Delo}");
                if (!silent)
                {
                    Console.ReadKey();
                }

                return 0;
            }

            long totalDownloadedSize = 0;

            var downloader = new DownloadService(new DownloadConfiguration()
            {
                ChunkCount = 1,
                MaxTryAgainOnFailover = 2,
                ParallelDownload = false,
                RequestConfiguration =
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    KeepAlive = false,
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:95.0) Gecko/20100101 Firefox/95.0",
                    Referer = pageData.PageMainUrl,
                }
            });

            EventHandler<Downloader.DownloadStartedEventArgs> onStartHandler = (sender, e) =>
            {
                totalDownloadedSize += e.TotalBytesToReceive;
            };

            downloader.DownloadStarted += onStartHandler;

            var progressBarOptions = new ProgressBarOptions
            {
                ProgressCharacter = '=',
                ProgressBarOnBottom = true,
                CollapseWhenFinished = true
            };
                        

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

                    return 0;
                }
            }

            var progressBarString = "{0}/{1} Фонд #{2} Опись #{3} Дело #{4}";
            var progressBarStringFormated = string.Format(progressBarString, record.Start, record.End, record.Fond, record.Opis, record.Delo);

            using (var mainProgressBar = new ProgressBar(realPageCount, progressBarStringFormated, progressBarOptions))
            {
                for (int i = record.Start; i <= record.End; i++)
                {
                    var url = $"https://cgamos.ru{pageData.PageUrls.ElementAt(i - 1)}";

                    var pbar = mainProgressBar.Spawn(100, $"Скачивание {url}", progressBarOptions);

                    EventHandler<Downloader.DownloadProgressChangedEventArgs> progressHandler = (sender, e) =>
                    {
                        var persents = (int)Math.Round(e.ProgressPercentage, 0);
                        pbar.Tick(persents);
                    };

                    downloader.DownloadProgressChanged += progressHandler;

                    try
                    {
                        var path = $"{directoryName}/{record.Fond}-{record.Opis}-{record.Delo.PadLeft(4, '0')}-{GetFileName(url)}";

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
                        downloader.DownloadProgressChanged -= progressHandler;
                        pbar.Dispose();
                    }
                }
            }

            downloader.DownloadStarted -= onStartHandler;

            return totalDownloadedSize;
        }

        private static string GetFileName(string url)
        {
            // '/images/MB_LS/01-0203-0745-000184/00000004.jpg' -> '0004.jpg'
            var index = url.LastIndexOf('/');

            return url.Substring(index + 5).ToLowerInvariant();
        }
    }

    internal record ArchiveRecord(string Fond, string Opis, string Delo, int Start = 1, int? End = null);

    internal record PageData(IReadOnlyCollection<string> PageUrls, int PageCount, string PageMainUrl);
}
