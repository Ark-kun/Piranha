using Mono.Cecil;
using System.Diagnostics;

namespace Ark.Cecil {
    public class ReferenceSearchingMetadataResolver : MetadataResolver {
        public ReferenceSearchingMetadataResolver(IAssemblyResolver assemblyResolver)
            : base(assemblyResolver) {
        }

        public override TypeDefinition Resolve(TypeReference type) {
            TypeDefinition result = null;
            try {
                result = TryResolve(type);
            } catch (AssemblyResolutionException) { }
            if (result != null) {
                return result;
            }
            var originalScope = type.Scope;
            foreach (var reference in type.Module.AssemblyReferences) {
                type.Scope = reference;
                try {
                    result = TryResolve(type);
                } catch (AssemblyResolutionException) { }
                if (result != null) {
                    Trace.WriteLine(string.Format("Successfully forwarded the type {0} from {1} to {2}.", type, originalScope, type.Scope), "ReferenceSearchingMetadataResolver");
                    return result;
                }
            }
            return null;
        }

        public TypeDefinition TryResolve(TypeReference type) {
            try {
                return base.Resolve(type);
            } catch (AssemblyResolutionException) { }
            return null;
        }
    }
}
