using CommandLine;
using CommandLine.Text;

namespace Piranha {
    class PiranhaCommands {
        public const string RemoveAllReferencesVerb = "remove-all-references";
        public const string MarkAllReferencesRetargetableVerb = "mark-all-references-retargetable";

        [VerbOption(RemoveAllReferencesVerb, HelpText = "Remove all references from the assembly")]
        public CommonOptions RemoveAllReferences { get; set; }

        [VerbOption(MarkAllReferencesRetargetableVerb, HelpText = "Marks all references as retargetable")]
        public CommonOptions MarkAllReferencesRetargetable { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb) {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
