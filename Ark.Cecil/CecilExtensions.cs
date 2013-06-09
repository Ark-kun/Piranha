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

        public static bool IsEqualTo(this ArrayType a, ArrayType b) {
            if (a.Rank != b.Rank) {
                return false;
            }
            return true;
        }

        public static bool IsEqualTo(this GenericInstanceType a, GenericInstanceType b) {
            if (a.GenericArguments.Count != b.GenericArguments.Count) {
                return false;
            }
            for (int i = 0; i < a.GenericArguments.Count; i++) {
                if (!IsEqualTo(a.GenericArguments[i], b.GenericArguments[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsEqualTo(this GenericParameter a, GenericParameter b) {
            return (a.Position == b.Position);
        }

        public static bool IsEqualTo(this IModifierType a, IModifierType b) {
            return IsEqualTo(a.ModifierType, b.ModifierType);
        }

        public static bool IsEqualTo(this TypeReference a, TypeReference b) {
            if (object.ReferenceEquals(a, b)) {
                return true;
            }
            if (a == null && b == null) {
                return true;
            }
            if ((a == null) || (b == null)) {
                return false;
            }
            //if (a.etype != b.etype) {
            //    return false;
            //}
            if (a.IsGenericParameter) {
                return IsEqualTo((GenericParameter)a, (GenericParameter)b);
            }
            //if (a.IsTypeSpecification()) {
            //    return AreSame((TypeSpecification)a, (TypeSpecification)b);
            //}
            return ((!(a.Name != b.Name) && !(a.Namespace != b.Namespace)) && IsEqualTo(a.DeclaringType, b.DeclaringType));
        }

        public static bool IsEqualTo(this TypeSpecification a, TypeSpecification b) {
            if (!IsEqualTo(a.ElementType, b.ElementType)) {
                return false;
            }
            if (a.IsGenericInstance) {
                return IsEqualTo((GenericInstanceType)a, (GenericInstanceType)b);
            }
            if (a.IsRequiredModifier || a.IsOptionalModifier) {
                return IsEqualTo((IModifierType)a, (IModifierType)b);
            }
            if (a.IsArray) {
                return IsEqualTo((ArrayType)a, (ArrayType)b);
            }
            return true;
        }

        public static bool IsEqualTo(this IList<ParameterDefinition> a, IList<ParameterDefinition> b) {
            int count = a.Count;
            if (count != b.Count) {
                return false;
            }
            if (count != 0) {
                for (int i = 0; i < count; i++) {
                    if (!IsEqualTo(a[i].ParameterType, b[i].ParameterType)) {
                        return false;
                    }
                }
            }
            return true;
        }

        public static void EmitBaseCallWithDefaultArgumentValues(this MethodBody methodBody, MethodReference methodRef) {
            if (methodRef != null && methodRef.HasGenericParameters || methodRef.DeclaringType.HasGenericParameters) {
                throw new ArgumentException("Method reference must not point to a method of a non-closed gereric type.", "methodRef");
            }
            methodBody.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            methodBody.EmitPushDefaultArgumentValues(methodRef);
            methodRef = methodBody.Method.Module.Import(methodRef);
            methodBody.Instructions.Add(Instruction.Create(OpCodes.Call, methodRef));
        }

        static void EmitPushDefaultArgumentValues(this MethodBody methodBody, MethodReference methodRef) {
            if (methodRef != null && methodRef.HasGenericParameters || methodRef.DeclaringType.HasGenericParameters) {
                throw new ArgumentException("Method reference must not point to a method of a non-closed gereric type.", "methodRef");
            }
            foreach (var parameter in methodRef.Parameters) {
                var parameterType = parameter.ParameterType;
                methodBody.EmitPushDefaultValue(parameterType);
            }
        }

        //Procuces invalid code for generic value types.
        public static void EmitPushDefaultValue(this MethodBody body, TypeReference type) {
            if (type.IsValueType) {
                body.InitLocals = true;
                var variableDef = new VariableDefinition(type);
                body.Variables.Add(variableDef);

                var il = body.GetILProcessor();
                il.Emit(OpCodes.Ldloca, variableDef);
                il.Emit(OpCodes.Initobj, variableDef.VariableType);
                il.Emit(OpCodes.Ldloc, variableDef);
            } else {
                body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
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
                    //where callMethodDef != null && callMethodDef.IsConstructor && (callMethodDef.DeclaringType.IsEqualTo(typeDef.BaseType) || callMethodDef.DeclaringType.IsEqualTo(typeDef)) //Some types had callMethodRef.DeclaringType == null
                    where callMethodDef != null && callMethodDef.IsConstructor && (callMethodRef.DeclaringType.IsEqualTo(typeDef.BaseType) || callMethodRef.DeclaringType.IsEqualTo(typeDef))
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