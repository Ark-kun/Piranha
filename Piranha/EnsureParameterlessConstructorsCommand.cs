using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class EnsureParameterlessConstructorsCommand : CommonCommand {
        public override void Execute() {
            new EnsureParameterlessConstructorsProcessor().ProcessAssemblyFromFile(Input, Output);
        }
    }
}
