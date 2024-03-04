using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace cgamos
{
    internal static class DataParser
    {

        public static async ValueTask<PageData> GetPageData(ArchiveRecord record)
        {
            var client = new HttpClient();

            var urlVariants = UrlMutatuinHelper.GetUrlVariants(record).ToArray();

            foreach (var urlVariant in urlVariants)
            {
                try
                {
                    var body = await client.GetStringAsync(urlVariant);

                    var pageUrls = GetPageUrls(body);
                    if (!pageUrls.Any())
                    {
                        continue;
                    }

                    return new PageData(pageUrls, pageUrls.Count, urlVariant);
                }
                catch (HttpRequestException)
                {
                    continue;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;

        }

        private static IReadOnlyCollection<string> GetPageUrls(string body)
        {
            var urls = new List<string>();
            const string dataSrcToken = "<img data-src=\"";
            const string resizeToken = "resize";

            var position = 0;
            while (position >= 0)
            {
                //<li class="swiper-slide" data-count="5" style="width: 596px; margin-right: 12px;" role="group" aria-label="6 / 1612">
                //<img data-src="/images/MB_LS/01-0203-0745-001743/00000006.jpg">
                //</li>
                
                position = body.IndexOf(dataSrcToken, position + dataSrcToken.Length);
                if (position == -1)
                {
                    break;
                }

                var start = position + dataSrcToken.Length;
                var end = body.IndexOf("\"", start);

                var str = body[start..end];

                if (str.Contains(resizeToken))
                {
                    continue;
                }
                
                urls.Add(str);
            }

            return urls;
        }
    }
}