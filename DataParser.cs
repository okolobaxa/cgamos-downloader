using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace cgamos
{
    internal static class DataParser
    {
        private static readonly HashSet<string> MetricBookFonds = new HashSet<string> 
        {
            "203", "520", "592", "1607", "1813", "2055", "2121", "2122", "2123", "2124", "2125", "2126", "2127", "2130", "2131", "2132", "2395"
        };

        public static async ValueTask<PageData> GetPageData(ArchiveRecord record)
        {
            try
            {
                var client = new HttpClient();
                string url = record.Fond switch 
                {
                    "203" when record.Opis == "747" => $"https://cgamos.ru/ispovedalnye_vedomosti/{record.Fond}-{record.Opis}-{record.Delo}/",
                    "51" => $"https://cgamos.ru/skazki/{record.Fond}-{record.Opis}-{record.Delo}/",
                    "2200" => $"https://cgamos.ru/inye-konfessii/islam/{record.Fond}-{record.Opis}-{record.Delo}/",
                    "2125" => $"https://cgamos.ru/ispovedalnye_vedomosti/{record.Fond}-{record.Opis}-{record.Delo}/",
                    _ when MetricBookFonds.Contains(record.Fond) => $"https://cgamos.ru/metric-books/{record.Fond}/{record.Fond}-{record.Opis}/{record.Fond}_{record.Opis}_{record.Delo}/",
                    _ => throw new ArgumentOutOfRangeException(record.Fond)
                };
                
                var body = await client.GetStringAsync(url);

                var pageCount = GetPageCount(body);
                if (pageCount == 0)
                {
                    return null;
                }

                var urls = GetUrls(body, pageCount);

                return new PageData(urls.ToArray(), pageCount);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IReadOnlyCollection<string> GetUrls(string body, int pageCount)
        {
            var urls = new List<string>(pageCount);
            const string token = "data-original=\"";

            var position = 0;
            while (position >= 0)
            {
                //<img data-original="/images/archive/01-0203-0780-001091/00000001.jpg" src="/images/archive/01-0203-0780-001091/00000001.jpg" >
                position = body.IndexOf(token, position + token.Length);
                if (position == -1) {
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
            //<input type="text" class="input-pages" data-max="216 " value="1">
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
