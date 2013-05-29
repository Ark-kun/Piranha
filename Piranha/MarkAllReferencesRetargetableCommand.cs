using Ark.Piranha;

namespace Piranha {
    class MarkAllReferencesRetargetableCommand : CommonCommand {
        public override void Execute() {
            new MarkAllReferencesRetargetableProcessor().ProcessAssemblyFromFile(Input, Output);
        }
    }
}
