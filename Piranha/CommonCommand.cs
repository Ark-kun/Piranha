using CommandLine;

namespace Piranha {
    abstract class CommonCommand {
        [Option('i', "input", HelpText = "Input assembly file." )]
        public string Input { get; set; }

        [Option('o', "output", HelpText = "Output assembly file.")]
        public string Output { get; set; }

        public abstract void Execute();
    }
}
