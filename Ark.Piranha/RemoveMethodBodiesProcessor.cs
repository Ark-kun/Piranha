using Ark.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class RemoveMethodBodiesProcessor : CecilProcessor {
        bool _fixConstructors;
        bool _fixFunctions;
        bool _fixVoidMethods;

        public RemoveMethodBodiesProcessor(bool fixConstructors = true, bool fixFunctions = true, bool fixVoidMethods = true) {
            _fixConstructors = fixConstructors;
            _fixFunctions = fixFunctions;
            _fixVoidMethods = fixVoidMethods;
        }

        public override void ProcessMethod(MethodDefinition methodDef) {
            var typeDef = methodDef.DeclaringType;
            var moduleDef = typeDef.Module;
            var typeSystem = moduleDef.TypeSystem;
            var voidTypeDef = typeSystem.Void;
            if (methodDef.HasBody) {
                var body = methodDef.Body;

                body.ExceptionHandlers.Clear();
                body.Variables.Clear();
                if (methodDef.IsConstructor && !methodDef.IsStatic && !typeDef.IsValueType) {
                    if (_fixConstructors) {
                        //Locating the end of the base constructor call.
                        var constructorRef = GetBaseConstructorCall(methodDef);
                        if (constructorRef != null) {
                            //Removing all other instructions other than the base class constructor call.
                            //FIX: We should just call the base constructor with default(type) arguments, but it's a bit too challenging for me right now (out, ref, generics etc), so we just preserve the existing call.
                            //FIX: We should just add (internal) parameterless constructors to all skeleton types that we have control over.
                            //while (callInstruction.Next != null) {
                            //    body.Instructions.Remove(callInstruction.Next);
                            //}
                            body.Instructions.Clear();
                            body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));

                            foreach (var parameter in constructorRef.Parameters) {
                                var parameterType = parameter.ParameterType;
                                //var parameterTypeRef = moduleDef.Import(parameterType);
                                //////if (!parameterType.IsGenericParameter) {
                                //////    if (parameterType.IsGenericInstance) {
                                //////        var genericType = parameterType.Resolve();
                                //////        moduleDef.Import(genericType);
                                //////    } else {
                                //////        parameterType = moduleDef.Import(parameterType);
                                //////    }
                                //////}
                                body.EmitDefaultInitializedVariable(parameter.Name, parameterType);
                            }

                            //body.Instructions.Add(Instruction.Create(OpCodes.Call, constructor));
                            //body.Instructions.Add(callInstruction);
                            body.Instructions.Add(Instruction.Create(OpCodes.Call, constructorRef));
                            //FIX!!!: usedConstructors.Add(constructorRef); //Workaround to prevent removal of used internal constructors.
                        } else {
                            Debug.WriteLine(string.Format("Strange: Constructor {0} doesn't call base type ({1}) constructor.", methodDef, methodDef.DeclaringType.BaseType));
                        }
                        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    } else {
                        body.Instructions.Clear();
                    }
                } else {
                    body.Instructions.Clear();

                    var il = body.GetILProcessor();
                    if (methodDef.ReturnType != voidTypeDef) {
                        if (_fixFunctions) {
                            body.InitLocals = true;
                            var variableDef = new VariableDefinition("result", methodDef.ReturnType);
                            body.Variables.Add(variableDef);

                            il.Emit(OpCodes.Ldloca, variableDef);
                            il.Emit(OpCodes.Initobj, variableDef.VariableType);
                            il.Emit(OpCodes.Ldloc, variableDef);
                            il.Emit(OpCodes.Ret);
                        }
                    } else {
                        if (_fixVoidMethods) {
                            il.Emit(OpCodes.Ret);
                        }
                    }
                }
            }
            base.ProcessMethod(methodDef);
        }

        //!!!!!!!!!!!!!!!!!!!!!!!!!! remove
        static MethodReference GetBaseConstructorCall(MethodDefinition methodDef, bool traverseConstructorChaining = true) {
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
                Debug.WriteLine(string.Format("Strange: Constructor {0} calls more than one base ({1}) or this ({2}) type constructors: {3}.", methodDef, methodDef.DeclaringType.BaseType, methodDef.DeclaringType), string.Join(", ", baseOrThisConstructorCalls.Select(cc => cc.ConstructorDef.ToString())));
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
