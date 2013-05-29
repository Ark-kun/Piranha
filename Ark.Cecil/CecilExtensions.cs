using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Cecil {
    public static class CecilExtensions {
        public static IEnumerable<TypeReference> GetAllUsedTypes(this AssemblyDefinition assemblyDef) {
            foreach (var typeDef in assemblyDef.GetTypesIncludingNested()) {
                foreach (var someTypeDef in GetAllUsedTypes(typeDef)) {
                    yield return someTypeDef;
                }
            }
        }

        public static IEnumerable<TypeReference> GetAllUsedTypes(this TypeDefinition typeDef) {
            yield return typeDef;

            if (typeDef.BaseType != null) {
                yield return typeDef.BaseType;
            }

            foreach (var fieldDef in typeDef.Fields) {
                yield return fieldDef.FieldType;
            }

            foreach (var methodDef in typeDef.Methods) {
                foreach (var someTypeDef in GetAllUsedTypes(methodDef)) {
                    yield return someTypeDef;
                }
            }
        }

        public static IEnumerable<TypeReference> GetAllUsedTypes(this MethodDefinition methodDef) {
            yield return methodDef.ReturnType;
            foreach (var parameter in methodDef.Parameters) {
                yield return parameter.ParameterType;
            }
            if (methodDef.HasBody) {
                var body = methodDef.Body;
                foreach (var variable in body.Variables) {
                    yield return variable.VariableType;
                }
                foreach (var instruction in body.Instructions) {
                    if (instruction.OpCode == OpCodes.Newobj) {
                        var newObjTypeRef = ((MemberReference)instruction.Operand).DeclaringType;
                        yield return newObjTypeRef;
                    }
                    if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt) {
                        var callMethodRef = instruction.Operand as MethodReference;
                        yield return callMethodRef.DeclaringType;
                    }
                }
            }
        }

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

        //Procuces invalid code for generic types.
        public static void EmitDefaultInitializedVariable(this MethodBody body, string name, TypeReference type) {
            body.InitLocals = true;
            var variableDef = new VariableDefinition(name, type);
            body.Variables.Add(variableDef);

            var il = body.GetILProcessor();
            il.Emit(OpCodes.Ldloca, variableDef);
            il.Emit(OpCodes.Initobj, variableDef.VariableType);
            il.Emit(OpCodes.Ldloc, variableDef);
        }

        public static MethodReference GetBaseConstructorCall(this MethodDefinition methodDef, bool traverseConstructorChaining = true) {
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
    }
}