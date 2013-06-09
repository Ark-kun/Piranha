using CommandLine;
using CommandLine.Text;

namespace Piranha {
    class PiranhaCommands {
        [VerbOption("remove-all-references", HelpText = "Remove all references from the assembly")]
        public RemoveAllReferencesCommand RemoveAllReferences { get; set; }

        [VerbOption("remove-all-resources", HelpText = "Remove all assembly resources")]
        public RemoveAllResourcesCommand RemoveAllResources { get; set; }

        [VerbOption("ensure-parameterless-constructors", HelpText = "Adds constructors to ensure that every type has public, protected or internal parameterless constructor.")]
        public EnsureParameterlessConstructorsCommand EnsureParameterlessConstructors { get; set; }

        [VerbOption("remove-method-bodies", HelpText = "Remove all code")]
        public RemoveMethodBodiesCommand RemoveMethodBodies { get; set; }

        [VerbOption("remove-pinvoke-methods", HelpText = "Remove all P/Invoke methods")]
        public RemovePInvokeMethodsCommand RemovePInvokeMethodsCommand { get; set; }

        [VerbOption("remove-private-members", HelpText = "Remove private class members")]
        public RemovePrivateMembersCommand RemovePrivateMembers { get; set; }

        [VerbOption("remove-private-types", HelpText = "Remove private types")]
        public RemovePrivateTypesCommand RemovePrivateTypes { get; set; }

        [VerbOption("make-skeleton", HelpText = "Remove all code, private class members and private types.")]
        public MakeSkeletonCommand MakeSkeleton { get; set; }

        [VerbOption("remove-external-types-usage", HelpText = "Remove members or types that expose non external (non-BCL) types.")]
        public RemoveExternalTypesUsageCommand RemoveExternalTypesUsage { get; set; }

        [VerbOption("mark-all-references-retargetable", HelpText = "Marks all references as retargetable")]
        public MarkAllReferencesRetargetableCommand MarkAllReferencesRetargetable { get; set; }

        [VerbOption("set-target-framework", HelpText = "Set TargetFramework attribute of the assembly")]
        public SetTargetFrameworkCommand SetTargetFramework { get; set; }

        [VerbOption("retarget-references", HelpText = "Retargets references to a new profile")]
        public RetargetReferencesCommand RetargetReferences { get; set; }
        
        [VerbOption("retarget-assembly", HelpText = "Retargets references to a new profile and changes the TargetProfile attribute")]
        public RetargetAssemblyCommand RetargetAssembly { get; set; }

        [VerbOption("make-portable-skeleton", HelpText = "Remove all resources, code, private class members, private types, changes the assembly framework profile and removes all dependencies on external types.")]
        public MakePortableSkeletonCommand MakePortableSkeleton { get; set; }

        [VerbOption("list-used-types", HelpText = "List all types used by the assembly")]
        public ListUsedTypesCommand ListUsedTypes { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb) {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
