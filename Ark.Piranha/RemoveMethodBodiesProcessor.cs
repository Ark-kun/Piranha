using Ark.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class RemoveMethodBodiesProcessor : CecilProcessor {
        bool _fixConstructors;
        bool _fixFunctions;
        bool _fixVoidMethods;
        HashSet<MethodReference> _usedConstructors = new HashSet<MethodReference>(CecilEqualityComparer.Default);

        public RemoveMethodBodiesProcessor(bool fixConstructors = true, bool fixFunctions = true, bool fixVoidMethods = true) {
            _fixConstructors = fixConstructors;
            _fixFunctions = fixFunctions;
            _fixVoidMethods = fixVoidMethods;
        }

        public ISet<MethodReference> UsedConstructors {
            get { return _usedConstructors; }
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
                        //Searching for a parameterless constructor of the base type.
                        //If it's not found we search the instructions to locate a call to the base constructor and use that.
                        TypeReference baseTypeRef = typeDef.BaseType;

                        MethodReference baseConstructorRef = null;
                        if (baseTypeRef != null) {
                            var baseTypeDef = baseTypeRef.TryResolve();
                            if(baseTypeDef != null) {
                                var baseConstructorDef = baseTypeDef.GetParameterlessConstructor();
                                if(baseConstructorDef != null) {
                                    baseConstructorRef = (baseTypeRef.IsGenericInstance ? baseConstructorDef.AsMethodOfGenericTypeInstance((GenericInstanceType)baseTypeRef) : baseConstructorDef);
                                }
                            }
                        }
                        if (baseConstructorRef == null) {
                            baseConstructorRef = methodDef.GetBaseConstructorCall();
                            if (baseConstructorRef == null) {
                                Debug.WriteLine(string.Format("Strange: Constructor {0} doesn't call base type ({1}) constructor.", methodDef, methodDef.DeclaringType.BaseType));
                            }
                        }
                        if (baseConstructorRef != null) {
                            body.Instructions.Clear();
                            body.EmitBaseCallWithDefaultArgumentValues(baseConstructorRef);
                            _usedConstructors.Add(baseConstructorRef); //Workaround to prevent removal of used internal constructors.
                        } else {
                            throw new Exception(string.Format("Couldn't find base constructor to call from {0} in the {1} base class.", methodDef, baseTypeRef));
                        }
                        body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    } else {
                        body.Instructions.Clear();
                    }
                } else {
                    body.Instructions.Clear();
                    var il = body.GetILProcessor();
                    var returnType = methodDef.ReturnType;
                    if (returnType != voidTypeDef) {
                        if (_fixFunctions) {
                            if (returnType.IsValueType || returnType.IsGenericParameter) {
                                body.InitLocals = true;
                                var variableDef = new VariableDefinition("result", returnType);
                                body.Variables.Add(variableDef);

                                il.Emit(OpCodes.Ldloca, variableDef);
                                il.Emit(OpCodes.Initobj, variableDef.VariableType);
                                il.Emit(OpCodes.Ldloc, variableDef);
                            } else {
                                il.Emit(OpCodes.Ldnull);
                            }
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
    }
}
