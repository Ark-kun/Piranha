using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemovePrivateTypesCommand : CommonCommand {
        public override void Execute() {
            new RemovePrivateTypesProcessor().ProcessAssemblyFromFile(Input, Output);
        }
    }
}
