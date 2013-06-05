using Mono.Cecil;

namespace Ark.Cecil {
    public class ReferenceSearchingMetadataResolver : MetadataResolver {
        public ReferenceSearchingMetadataResolver(IAssemblyResolver assemblyResolver)
            : base(assemblyResolver) {
        }

        public override TypeDefinition Resolve(TypeReference type) {
            var result = base.Resolve(type);
            if (result != null) {
                return result;
            }

            foreach (var reference in type.Module.AssemblyReferences) {
                type.Scope = reference;
                result = base.Resolve(type);
                if (result != null) {
                    return result;
                }
            }
            return null;
        }
    }
}
