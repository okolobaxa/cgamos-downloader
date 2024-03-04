using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cgamos
{
    internal static class UrlMutatuinHelper
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

        private readonly static string[] UrlTemplates = new string[]
        {
            "{0}-{1}-{2}/",
            "{0}_{1}_{2}/",
            "{0}/{0}-{1}/{0}_{1}_{2}/",
            "{0}/{0}-{1}/{0}-{1}-{2}/"
        };

        private static readonly Dictionary<char, char> CharReplacements = new Dictionary<char, char>
        {
            // ru to en lower case
            {'а', 'a'},
            {'о', 'o'},
            {'с', 'c'},
            {'А', 'a'},
            {'О', 'o'},
            {'С', 'c'},

            // en to ru upper case
            {'a', 'а'},
            {'o', 'о'},
            {'c', 'с'},
            {'A', 'а'},
            {'O', 'о'},
            {'C', 'с'},
        };

        public static IEnumerable<string> GetUrlVariants(ArchiveRecord record)
        {

            var combinations = from directory in Directories
                               from urlTemplate in UrlTemplates
                               from mutation in Mutate(directory, urlTemplate, record)
                               select mutation;

            return combinations;
        }

        private static IEnumerable<string> Mutate(string directory, string urlTemplate, ArchiveRecord record)
        {
            //add users's input as is
            yield return $"{directory}{string.Format(urlTemplate, record.Fond, record.Opis, record.Delo)}";

            if (record.Delo.Any(x => char.IsLetter(x)))
            {
                var sb = new StringBuilder(record.Delo);

                // add cyrylic <-> latin or latin <-> cyrylic replacemtns
                if (TryGetLangReplacement(record.Delo, out var langReplacement))
                {
                    //Add low-case variant of replacement (default)
                    sb[langReplacement.position] = langReplacement.letter;
                    yield return $"{directory}{string.Format(urlTemplate, record.Fond, record.Opis, sb.ToString())}";

                    //Add upper-case variant of replacement
                    sb[langReplacement.position] = char.ToUpperInvariant(langReplacement.letter);
                    yield return $"{directory}{string.Format(urlTemplate, record.Fond, record.Opis, sb.ToString())}";
                }
                // add lower <-> upper or upper <-> lower replacements
                else if (TryGetCaseReplacement(record.Delo, out var caseReplacement))
                {
                    sb[caseReplacement.position] = caseReplacement.letter;
                    yield return $"{directory}{string.Format(urlTemplate, record.Fond, record.Opis, sb.ToString())}";
                }
            }
        }

        private static bool TryGetLangReplacement(string str, out (int position, char letter) replacement)
        {
            for (int i = str.Length - 1; i >= 0; i--)
            {
                char ch = str[i];

                if (char.IsLetter(ch) && CharReplacements.TryGetValue(ch, out var replacedChar))
                {
                    replacement = (i, replacedChar);
                    return true;
                }
            }

            replacement = default;

            return false;
        }

        private static bool TryGetCaseReplacement(string str, out (int position, char letter) replacement)
        {
            for (int i = str.Length - 1; i >= 0; i--)
            {
                char ch = str[i];

                if (char.IsLetter(ch))
                {
                    if (char.IsLower(ch))
                    {
                        replacement = (i, char.ToUpperInvariant(ch));
                    }
                    else
                    {
                        replacement = (i, char.ToLowerInvariant(ch));
                    }

                    return true;
                }
            }

            replacement = default;

            return false;
        }
    }
}