using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class EnsureParameterlessConstructorsCommand : CommonCommand {
        [Option('a', "all-types", HelpText = "Add parameterless constructors to all types (not only classes that have derived classes).")]
        public bool AllTypes { get; set; }

        public override void Execute() {
            new EnsureParameterlessConstructorsProcessor(AllTypes).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
