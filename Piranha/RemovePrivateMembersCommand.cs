using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemovePrivateMembersCommand : CommonCommand {
        [Option("preserve-fields-of-structs", HelpText = "Don't remove private fields of structures.")]
        public bool PreserveFieldsOfStructs { get; set; }

        [Option("leave-more-internal-constructors", HelpText = "Don't remove internal constructors with parameters if the type lacks a parameterless or public constructor.")]
        public bool LeaveSomeInternalConstructorsWithParameters { get; set; }

        public override void Execute() {
            new RemovePrivateMembersProcessor(PreserveFieldsOfStructs, LeaveSomeInternalConstructorsWithParameters).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
