using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;

namespace Ark.Piranha {
    public class MakePortableSkeletonProcessor : CecilProcessor {
        DefaultAssemblyResolver _assemblyResolver = new DefaultAssemblyResolver();
        FrameworkProfile _frameworkProfile;

        public MakePortableSkeletonProcessor(FrameworkProfile frameworkProfile) {
            _frameworkProfile = frameworkProfile;
        }

        protected override ReaderParameters GetDefaultReaderParameters() {
            return new ReaderParameters() { MetadataResolver = new ReferenceSearchingMetadataResolver(_assemblyResolver) };
        }

        public override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            new MakeSkeletonProcessor(false).ProcessAssembly(assemblyDef);
            new RemovePInvokeMethodsProcessor().ProcessAssembly(assemblyDef);
            _assemblyResolver.AddSearchDirectory(_frameworkProfile.ReferencesDirectory);
            new RetargetAssemblyProcessor(_frameworkProfile, true).ProcessAssembly(assemblyDef);
        }
    }
}
