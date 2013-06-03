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
            new RetargetReferencesProcessor(_frameworkProfile.GetFrameworkAssemblies(), _removeOtherReferences).ProcessAssemblyReferences(assemblyDef.MainModule, assemblyDef.MainModule.AssemblyReferences);
            base.ProcessAssembly(assemblyDef);
        }

        public override void ProcessCustomAssemblyAttributes(AssemblyDefinition assemblyDef, IList<CustomAttribute> attributes) {
            new SetTargetFrameworkProcessor(_frameworkProfile).ProcessCustomAssemblyAttributes(assemblyDef, attributes);
            base.ProcessCustomAssemblyAttributes(assemblyDef, attributes);
        }

        public override void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            //new RetargetReferencesProcessor(_frameworkProfile.GetFrameworkAssemblies(), _removeOtherReferences).ProcessAssemblyReferences(moduleDef, assemblyNameRefs);
            base.ProcessAssemblyReferences(moduleDef, assemblyNameRefs);
        }
    }
}
