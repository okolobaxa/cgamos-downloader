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
            const string token = "data-original=\"";

            var position = 0;
            while (position >= 0)
            {
                //<img data-original="/images/archive/01-0203-0780-001091/00000001.jpg" src="/images/archive/01-0203-0780-001091/00000001.jpg" >
                position = body.IndexOf(token, position + token.Length);
                if (position == -1)
                {
                    break;
                }

                var start = position + token.Length;
                var end = body.IndexOf("\"", start);

                var str = body[start..end];

                urls.Add(str);
            }

            return urls;
        }

        private static short GetPageCount(string body)
        {
            //<input type="text" class="current-picture__num input-pages" data-max="1345 " value="1">
            var index = body.IndexOf("data-max=\"");
            if (index == -1)
            {
                return 0;
            }

            var start = index + "data-max=\"".Length;
            var end = body.IndexOf(" \"", start);

            var str = body[start..end];

            if (short.TryParse(str.Trim(), out var number))
            {
                return number;
            }

            return 0;
        }
    }
}