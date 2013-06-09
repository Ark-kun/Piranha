using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Piranha {
    public class RetargetAssemblyProcessor : CecilProcessor {
        FrameworkProfile _frameworkProfile;
        bool _removeOtherReferences;

        public RetargetAssemblyProcessor(string frameworkProfile, bool removeOtherReferences = false)
            : this(FrameworkProfile.Parse(frameworkProfile), removeOtherReferences) {
        }

        public RetargetAssemblyProcessor(FrameworkProfile frameworkProfile, bool removeOtherReferences = false) {
            _frameworkProfile = frameworkProfile;
            _removeOtherReferences = removeOtherReferences;
        }

        public override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            ((DefaultAssemblyResolver)assemblyDef.MainModule.AssemblyResolver).AddSearchDirectory(_frameworkProfile.ReferencesDirectory);
            new SetTargetFrameworkProcessor(_frameworkProfile).ProcessAssembly(assemblyDef);
            new RetargetReferencesProcessor(_frameworkProfile.GetFrameworkAssemblies(), _removeOtherReferences).ProcessAssembly(assemblyDef);
            new RemoveExternalTypesUsageProcessor(_frameworkProfile).ProcessAssembly(assemblyDef);
        }
    }
}
