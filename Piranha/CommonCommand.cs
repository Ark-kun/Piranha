using CommandLine;

namespace Piranha {
    abstract class CommonCommand {
        [Option('i', "input", Required = true, HelpText = "Input assembly file." )]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output assembly file.")]
        public string Output { get; set; }

        public abstract void Execute();
    }
}
