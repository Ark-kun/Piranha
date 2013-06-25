using Mono.Cecil;
using System.Linq;

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

        public static FieldDefinition TryResolve(this IMetadataResolver metadataResolver, FieldReference fieldRef) {
            try {
                return metadataResolver.Resolve(fieldRef);
            } catch (AssemblyResolutionException) { }
            return null;
        }

        public static MethodDefinition TryResolve(this IMetadataResolver metadataResolver, MethodReference methodRef) {
            try {
                return metadataResolver.Resolve(methodRef);
            } catch (AssemblyResolutionException) { }
            return null;
        }

        public static TypeDefinition TryResolve(this IMetadataResolver metadataResolver, TypeReference typeRef) {
            try {
                return metadataResolver.Resolve(typeRef);
            } catch (AssemblyResolutionException) { }
            return null;
        }

        /// <summary>
        /// Resolves the type reference in the specified module.
        /// </summary>
        /// <param name="scope">The module where the type reference is resolved.</param>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <returns>The resolved type definition.</returns>
        public static TypeDefinition TryResolve(this ModuleDefinition scope, TypeReference typeRef) {
            var matchingTypeRef = typeRef.Clone();
            matchingTypeRef.Scope = scope;
            return scope.MetadataResolver.TryResolve(matchingTypeRef);
        }

        public static EventDefinition TryResolve(this ModuleDefinition scope, EventReference eventRef) {
            var matchingType = TryResolve(scope, eventRef.DeclaringType);
            if (matchingType == null) {
                return null;
            }
            return matchingType.Events.FirstOrDefault(e => e.Name == eventRef.Name);
        }

        public static PropertyDefinition TryResolve(this ModuleDefinition scope, PropertyReference propertyRef) {
            var matchingType = TryResolve(scope, propertyRef.DeclaringType);
            if (matchingType == null) {
                return null;
            }
            return matchingType.Properties.FirstOrDefault(e => e.Name == propertyRef.Name);
        }

        public static FieldDefinition TryResolve(this ModuleDefinition scope, FieldReference fieldRef) {
            var matchingTypeRef = fieldRef.DeclaringType.Clone();
            matchingTypeRef.Scope = scope;
            var matchingFieldRef = fieldRef.Clone();
            matchingFieldRef.DeclaringType = matchingTypeRef;
            return scope.MetadataResolver.TryResolve(matchingFieldRef);
        }

        public static MethodDefinition TryResolve(this ModuleDefinition scope, MethodReference methodRef) {
            var matchingTypeRef = methodRef.DeclaringType.Clone();
            matchingTypeRef.Scope = scope;
            var matchingMethodRef = methodRef.Clone();
            matchingMethodRef.DeclaringType = matchingTypeRef;
            return scope.MetadataResolver.TryResolve(matchingMethodRef);
        }
    }
}