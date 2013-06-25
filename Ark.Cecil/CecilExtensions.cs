using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Cecil {
    public static class CecilExtensions {
        public static IEnumerable<TypeDefinition> GetTypesIncludingNested(this AssemblyDefinition assemblyDef) {
            foreach (var moduleDef in assemblyDef.Modules) {
                foreach (var someTypeDef in moduleDef.GetTypesIncludingNested()) {
                    yield return someTypeDef;
                }
            }
        }

        public static IEnumerable<TypeDefinition> GetTypesIncludingNested(this ModuleDefinition moduleDef) {
            foreach (var typeDef in moduleDef.Types) {
                yield return typeDef;
                foreach (var nestedTypeDef in typeDef.GetAllNestedTypes()) {
                    yield return nestedTypeDef;
                }
            }
        }

        public static IEnumerable<TypeDefinition> GetTypeAndItsNestedTypes(this TypeDefinition rootTypeDef) {
            yield return rootTypeDef;
            foreach (var nestedTypeDef in rootTypeDef.GetAllNestedTypes()) {
                yield return nestedTypeDef;
            }
        }

        public static IEnumerable<TypeDefinition> GetAllNestedTypes(this TypeDefinition rootTypeDef) {
            foreach (var nestedTypeDef in rootTypeDef.NestedTypes) {
                yield return nestedTypeDef;
                foreach (var typeDef in nestedTypeDef.GetAllNestedTypes()) {
                    yield return typeDef;
                }
            }
        }

        public static IEnumerable<MethodReference> GetBaseConstructorCalls(this TypeDefinition typeDef) {
            return typeDef.Methods.Where(methodDef => methodDef.IsConstructor && !methodDef.IsStatic).Select(methodDef => methodDef.GetBaseConstructorCall()).Where<MethodReference>(methodDef => methodDef != null).Distinct<MethodReference>(CecilEqualityComparer.Default);
        }

        public static MethodReference GetBaseConstructorCall(this MethodDefinition methodDef, bool traverseConstructorChaining = true) {
            if (methodDef.Body == null) {
                return null;
            }
            var typeDef = methodDef.DeclaringType;
            var baseOrThisConstructorCalls = (
                    from instr in methodDef.Body.Instructions
                    where instr.OpCode == OpCodes.Call
                    let callMethodRef = (MethodReference)instr.Operand
                    let callMethodDef = callMethodRef.TryResolve()
                    where ((callMethodDef != null && callMethodDef.IsConstructor) || (callMethodRef.Name == ".ctor")) && (callMethodRef.DeclaringType.IsEqualTo(typeDef.BaseType) || callMethodRef.DeclaringType.IsEqualTo(typeDef))
                    select new { ConstructorRef = callMethodRef, ConstructorDef = callMethodDef }
                ).ToList();
            if (!baseOrThisConstructorCalls.Any()) {
                return null;
            }
            if (baseOrThisConstructorCalls.Count > 1) {
                System.Diagnostics.Debug.WriteLine(string.Format("Strange: Constructor {0} calls more than one base ({1}) or this ({2}) type constructors: {3}.", methodDef, methodDef.DeclaringType.BaseType, methodDef.DeclaringType), string.Join(", ", baseOrThisConstructorCalls.Select(cc => cc.ConstructorDef.ToString())));
            }
            var baseConstructorCall = baseOrThisConstructorCalls.First();
            if (baseConstructorCall.ConstructorRef.DeclaringType.IsEqualTo(typeDef.BaseType) || !traverseConstructorChaining) {
                return baseConstructorCall.ConstructorRef;
            } else {
                return GetBaseConstructorCall(baseConstructorCall.ConstructorDef, true);
            }
        }

        public static MethodDefinition GetParameterlessConstructor(this TypeDefinition typeDef) {
            return typeDef.Methods.SingleOrDefault(methodDef => methodDef.IsConstructor && !methodDef.HasParameters && !methodDef.IsStatic && !methodDef.IsPrivate);
        }

        public static GenericInstanceType MakeGenericInstanceType(this TypeReference typeRef, params TypeReference[] genericTypeArguments) {
            if (typeRef == null) {
                throw new ArgumentNullException("typeRef");
            }
            if (genericTypeArguments == null) {
                throw new ArgumentNullException("genericTypeArguments");
            }
            if (genericTypeArguments.Length == 0) {
                throw new ArgumentException("Generic type arguments list is empty", "genericTypeArguments");
            }
            if (typeRef.GenericParameters.Count != genericTypeArguments.Length) {
                throw new ArgumentException(string.Format("Got {0} generic type arguments instead of {1}", genericTypeArguments.Length, typeRef.GenericParameters.Count), "genericTypeArguments");
            }
            GenericInstanceType type = new GenericInstanceType(typeRef);
            foreach (TypeReference reference in genericTypeArguments) {
                type.GenericArguments.Add(reference);
            }
            return type;
        }

        public static MethodReference AsMethodOfGenericTypeInstance(this MethodDefinition methodDef, GenericInstanceType genericInstanceType) {
            if (!genericInstanceType.ElementType.IsEqualTo(methodDef.DeclaringType)) {
                throw new ArgumentException("The generic instance type doesn't match the method's declaring type.", "genericInstanceType");
            }
            var methodRef = methodDef.Clone();
            methodRef.DeclaringType = genericInstanceType;
            return methodRef;
        }

        public static MethodReference WithGenericTypeArguments(this MethodDefinition methodDef, params TypeReference[] genericTypeArguments) {
            return AsMethodOfGenericTypeInstance(methodDef, methodDef.DeclaringType.MakeGenericInstanceType(genericTypeArguments));
        }
    }
}