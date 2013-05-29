using Ark.Piranha;

namespace Piranha {
    class RemoveAllReferencesCommand : CommonCommand {
        public override void Execute() {
            new RemoveAllReferencesProcessor().ProcessAssemblyFromFile(Input, Output);
        }
    }
}
