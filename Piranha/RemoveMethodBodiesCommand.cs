using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemoveMethodBodiesCommand : CommonCommand {
        [Option("dont-fix-constructors", HelpText = "Don't add the code calling the base class constructor. Breaks verification.")]
        public bool DontFixConstructors { get; set; }

        [Option("dont-fix-functions", HelpText = "Don't add the code returning null result from the functions. Breaks verification.")]
        public bool DontFixFunctions { get; set; }

        [Option("dont-fix-void-methods", HelpText = "Don't add the ret instruction to void methods. Breaks verification.")]
        public bool DontFixVoidMethods { get; set; }

        public override void Execute() {
            new RemoveMethodBodiesProcessor(!DontFixConstructors, !DontFixFunctions, !DontFixVoidMethods).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
