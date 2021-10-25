using CommandLine;

namespace cgamos
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

        [Option('s', "start", Required = false, HelpText = "Start")]
        public short Start { get; set; }

        [Option('e', "end", Required = false, HelpText = "End")]
        public short? End { get; set; }
    }
}
