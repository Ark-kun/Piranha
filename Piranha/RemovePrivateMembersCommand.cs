using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemovePrivateMembersCommand : CommonCommand {
        [Option("preserve-fields-of-structs", HelpText = "Don't remove private fields of structures.")]
        public bool PreserveFieldsOfStructs { get; set; }

        public override void Execute() {
            new RemovePrivateMembersProcessor(PreserveFieldsOfStructs).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
