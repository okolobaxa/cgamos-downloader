using System;

namespace cgamos
{
    internal static class ConsoleInputParser
    {
        public static ArchiveRecord ParseInput()
        {
            var fond = ParseStringConsoleInput("Фонд #: ", "Неправильный # фонда");
            var opis = ParseStringConsoleInput("Опись #: ", "Неправильный # описи");
            var delo = ParseStringConsoleInput("Дело #: ", "Неправильный # дела", validateAsNumber: false);
            var pageStart = ParseNumberConsoleInput("Лист с: [Нажмити ENTER если 1] ", "Неправильный # листа", 1).Value;
            var pageEnd = ParseNumberConsoleInput("Лист по: [Нажмите ENTER если все] ", "Неправильный # листа", defaultValue: null);

            return new ArchiveRecord(fond, opis, delo, pageStart, pageEnd);
        }

        private static short? ParseNumberConsoleInput(string message, string errorMessage, short? defaultValue)
        {
            short? result = null;

            while (true)
            {
                Console.Write(message);

                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    return defaultValue;
                }

                if (short.TryParse(input, out var number) && number > 0)
                {
                    return result;
                }

                Console.WriteLine(errorMessage);
            }
        }

        private static string ParseStringConsoleInput(string message, string errorMessage, bool validateAsNumber = true)
        {
            bool askMore = true;

            while (askMore)
            {
                Console.Write(message); //"Fond #: "

                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) || (validateAsNumber && !short.TryParse(input, out var _)))
                {
                    Console.WriteLine(errorMessage); //"Invalid Fond #"
                }
                else
                {
                    return input;
                }
            }

            return null;
        }
    }
}