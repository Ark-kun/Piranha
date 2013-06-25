using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace Ark.Cecil {
    public static class EmitExtensions {
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
    }
}