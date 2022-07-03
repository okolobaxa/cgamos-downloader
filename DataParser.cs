using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace cgamos
{
    internal static class DataParser
    {
        private readonly static string[] Directories = new string[]
        {
            "https://cgamos.ru/metric-books/",
            "https://cgamos.ru/ispovedalnye_vedomosti/",
            "https://cgamos.ru/skazki/",
            "https://cgamos.ru/inye-konfessii/islam/",
            "https://cgamos.ru/inye-konfessii/iudaizm/",
            "https://cgamos.ru/inye-konfessii/catholicism/",
            "https://cgamos.ru/inye-konfessii/lutheranism/"
        };

        private static readonly Dictionary<char, char> ConvertedLetters = new Dictionary<char, char>
        {
            // ru to en
            {'а', 'a'},
            {'о', 'o'},
            {'с', 'c'},
            {'А', 'A'},
            {'О', 'O'},
            {'С', 'C'},

            // en to ru
            {'a', 'а'},
            {'o', 'о'},
            {'c', 'с'},
            {'A', 'А'},
            {'O', 'О'},
            {'C', 'С'},
        };

        public static async ValueTask<PageData> GetPageData(ArchiveRecord record)
        {
            var client = new HttpClient();

            var urlVariants = GetUrlVariants(record);

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

        private static IReadOnlyCollection<string> GetUrlVariants(ArchiveRecord record)
        {
            var containtReplacement = TryGetReplacement(record.Delo, out var replacement);

            string[] urlVariants;

            if (containtReplacement)
            {
                var sb = new StringBuilder(record.Delo);
                sb[replacement.position] = replacement.letter;
                var replaced = sb.ToString();

                urlVariants = new string[]
                {
                    $"{record.Fond}-{record.Opis}-{record.Delo}/",
                    $"{record.Fond}_{record.Opis}_{record.Delo}/",
                    $"{record.Fond}/{record.Fond}-{record.Opis}/{record.Fond}_{record.Opis}_{record.Delo}/",
                    
                    $"{record.Fond}-{record.Opis}-{replaced}/",
                    $"{record.Fond}_{record.Opis}_{replaced}/",
                    $"{record.Fond}/{record.Fond}-{record.Opis}/{record.Fond}_{record.Opis}_{replaced}/"
                };
            }
            else
            {
                urlVariants = new string[]
                {
                    $"{record.Fond}-{record.Opis}-{record.Delo}/",
                    $"{record.Fond}_{record.Opis}_{record.Delo}/",
                    $"{record.Fond}/{record.Fond}-{record.Opis}/{record.Fond}_{record.Opis}_{record.Delo}/"
                };
            }


            var combinations = from directory in Directories
                               from urlVariant in urlVariants
                               select $"{directory}{urlVariant}";

            return combinations.ToArray();
        }

        private static bool TryGetReplacement(string str, out (int position, char letter) replacement)
        {
            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];

                if (ConvertedLetters.TryGetValue(ch, out var replacedChar))
                {
                    replacement = (i, replacedChar);
                    return true;
                }
            }

            replacement = default;

            return false;
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