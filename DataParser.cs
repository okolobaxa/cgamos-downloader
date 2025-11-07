using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace cgamos
{
    internal static class DataParser
    {
        public static async ValueTask<PageData> GetPageData(ArchiveRecord record)
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler();
            handler.CookieContainer = cookieContainer;
            
            var client = new HttpClient(handler);

            var urlVariants = UrlMutatuinHelper.GetUrlVariants(record).ToArray();

            foreach (var urlVariant in urlVariants)
            {
                try
                {
                    var response = await client.GetAsync(urlVariant);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var body = await response.Content.ReadAsStringAsync();

                    var pageUrls = GetPageUrls(body);
                    if (!pageUrls.Any())
                    {
                        continue;
                    }

                    var cookies = cookieContainer.GetCookies(new Uri(urlVariant));

                    return new PageData(pageUrls, pageUrls.Count, urlVariant, cookies);
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
            const string dataSrcToken = "<img data-src='";
            const string resizeToken = "resize";

            var position = 0;
            while (position >= 0)
            {
                //<li class='swiper-slide' data-id='00000286' data-count='285'>
                //<img data-src='/ajax/get_img.php?img_path=RS_LS/01-2199-0001-000027/00000286.jpg' data-lazyload-src='/ajax/get_img.php?img_path=RS_LS/01-2199-0001-000027/00000286.jpg' src='/local/templates/mosarchiv_new/img/tube-spinner.svg' data-lazyload-error-src=''>
                //</li>
                
                position = body.IndexOf(dataSrcToken, position + dataSrcToken.Length);
                if (position == -1)
                {
                    break;
                }

                var start = position + dataSrcToken.Length;
                var end = body.IndexOf("'", start);

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