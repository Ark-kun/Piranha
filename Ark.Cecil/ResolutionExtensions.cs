using Mono.Cecil;

namespace Ark.Cecil {
    public static class ResolutionExtensions {
        public static AssemblyDefinition TryResolve(this IAssemblyResolver assemblyResolver, AssemblyNameReference assemblyNameRef) {
            try {
                return assemblyResolver.Resolve(assemblyNameRef);
            } catch (AssemblyResolutionException) { }
            return null;
        }

        public static AssemblyDefinition TryResolve(this ModuleDefinition moduleDef, AssemblyNameReference assemblyNameRef) {
            try {
                return moduleDef.AssemblyResolver.Resolve(assemblyNameRef);
            } catch (AssemblyResolutionException) { }
            return null;
        }

        public static TypeDefinition TryResolve(this ExportedType exportedType) {
            try {
                return exportedType.Resolve();
            } catch (AssemblyResolutionException) { }
            return null;
        }

        public static MethodDefinition TryResolve(this MethodReference methodRef) {
            try {
                return methodRef.Resolve();
            } catch (AssemblyResolutionException) { }
            return null;
        }

        public static TypeDefinition TryResolve(this TypeReference typeRef) {
            try {
                return typeRef.Resolve();
            } catch (AssemblyResolutionException) { }
            return null;
        }
    }
}