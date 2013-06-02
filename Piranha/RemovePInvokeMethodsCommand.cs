using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemovePInvokeMethodsCommand : CommonCommand {
        public override void Execute() {
            new RemovePInvokeMethodsProcessor().ProcessAssemblyFromFile(Input, Output);
        }
    }
}
