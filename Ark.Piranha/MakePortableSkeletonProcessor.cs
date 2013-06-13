using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;

namespace Ark.Piranha {
    public class MakePortableSkeletonProcessor : CecilProcessor {
        DefaultAssemblyResolver _assemblyResolver = new FrameworkAssemblyResolver();
        FrameworkProfile _frameworkProfile;

        public MakePortableSkeletonProcessor(FrameworkProfile frameworkProfile) {
            _frameworkProfile = frameworkProfile;
        }

        protected override ReaderParameters GetDefaultReaderParameters() {
            return new ReaderParameters() { MetadataResolver = new ReferenceSearchingMetadataResolver(_assemblyResolver) };
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            new MakeSkeletonProcessor(false).Process(assemblyDef);
            new RemovePInvokeMethodsProcessor().Process(assemblyDef);
            _assemblyResolver.AddSearchDirectory(_frameworkProfile.ReferencesDirectory);
            new RetargetAssemblyProcessor(_frameworkProfile, true).Process(assemblyDef);
        }
    }
}
