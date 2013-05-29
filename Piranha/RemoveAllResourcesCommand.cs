using Ark.Piranha;

namespace Piranha {
    class RemoveAllResourcesCommand : CommonCommand {
        public override void Execute() {
            new RemoveAllResourcesProcessor().ProcessAssemblyFromFile(Input, Output);
        }
    }
}
