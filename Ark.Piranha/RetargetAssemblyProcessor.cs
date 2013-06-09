using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Piranha {
    public class RetargetAssemblyProcessor : CecilProcessor {
        DefaultAssemblyResolver _assemblyResolver = new DefaultAssemblyResolver();
        FrameworkProfile _frameworkProfile;
        bool _removeOtherReferences;

        public RetargetAssemblyProcessor(string frameworkProfile, bool removeOtherReferences = false)
            : this(FrameworkProfile.Parse(frameworkProfile), removeOtherReferences) {
        }

        public RetargetAssemblyProcessor(FrameworkProfile frameworkProfile, bool removeOtherReferences = false) {
            _frameworkProfile = frameworkProfile;
            _removeOtherReferences = removeOtherReferences;
        }

        protected override ReaderParameters GetDefaultReaderParameters() {
            return new ReaderParameters() { MetadataResolver = new ReferenceSearchingMetadataResolver(_assemblyResolver) };
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            _assemblyResolver.AddSearchDirectory(_frameworkProfile.ReferencesDirectory);
            new SetTargetFrameworkProcessor(_frameworkProfile).Process(assemblyDef);
            new RetargetReferencesProcessor(_frameworkProfile.GetFrameworkAssemblies(), _removeOtherReferences).Process(assemblyDef);
            new RemoveExternalTypesUsageProcessor(_frameworkProfile).Process(assemblyDef);
        }
    }
}
