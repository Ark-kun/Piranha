using Ark.Cecil;
using Ark.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class EnsureParameterlessConstructorsProcessor : CecilProcessor {
        bool _addToAllTypes;
        HashSet<TypeDefinition> _baseTypes;

        public EnsureParameterlessConstructorsProcessor(bool addToAllTypes = false) {
            _addToAllTypes = addToAllTypes;
            if (!_addToAllTypes) {
                _baseTypes = new HashSet<TypeDefinition>(CecilEqualityComparer.Default);
            }
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            base.ProcessAssembly(assemblyDef);
            if (!_addToAllTypes) {
                _baseTypes.ForEach(EnsureParameterlessConstructor);
            }
        }

        protected override void ProcessType(TypeDefinition typeDef) {
            if (_addToAllTypes) {
                EnsureParameterlessConstructor(typeDef);
            } else {
                var baseTypeRef = typeDef.BaseType;
                if (baseTypeRef != null) {
                    var baseTypeDef = baseTypeRef.TryResolve();
                    if (baseTypeDef != null) {
                        if (typeDef.Module.Assembly.Modules.Contains(baseTypeDef.Module, CecilEqualityComparer.Default)) {
                            _baseTypes.Add(baseTypeDef);
                        }
                    }
                }
            }
            base.ProcessType(typeDef);
        }

        static void EnsureParameterlessConstructor(TypeDefinition typeDef) {
            //Skip delegates?
            if (typeDef.IsValueType || typeDef.IsInterface || typeDef.BaseType == null || typeDef.BaseType.FullName == "System.MulticastDelegate") {
                return;
            }
            if (typeDef.Name == "ModelBoneCollection") {
            }
            if (typeDef.BaseType.IsGenericInstance) {
            }
            if (typeDef.GetParameterlessConstructor() == null) {
                var baseTypeRef = typeDef.BaseType;
                var baseTypeDef = baseTypeRef.TryResolve();
                if (baseTypeDef == null) {
                    //that's bad...
                    //Searching neighbour constructors.
                    var baseConstructorRefs = typeDef.GetBaseConstructorCalls();
                    var baseConstructorRef = baseConstructorRefs.OrderBy(methodRef => methodRef.Parameters.Count).First(); //Selecting constructor with the fewest nember of parameters. It's not perfect,
                    AddParameterlessConstructor(typeDef, baseConstructorRef);
                } else {
                    var baseConstructorDef = baseTypeDef.GetParameterlessConstructor();
                    if (baseConstructorDef != null) {
                        var baseConstructorRef = (baseTypeRef.IsGenericInstance ? baseConstructorDef.AsMethodOfGenericTypeInstance((GenericInstanceType)baseTypeRef) : baseConstructorDef);
                        AddParameterlessConstructor(typeDef, baseConstructorRef);
                    } else {
                        if (baseTypeDef.Module == typeDef.Module) { //Is this equality comparison correct?
                            EnsureParameterlessConstructor(baseTypeDef);
                        } else {
                            var baseConstructorRefs = typeDef.GetBaseConstructorCalls();
                            var baseConstructorRef = baseConstructorRefs.OrderBy(methodRef => methodRef.Parameters.Count).First(); //Selecting constructor with the fewest nember of parameters. It's not perfect,
                            AddParameterlessConstructor(typeDef, baseConstructorRef);
                        }
                    }
                }
            }
        }

        static MethodDefinition AddParameterlessConstructor(TypeDefinition typeDef, MethodReference baseConstructorRef = null) {
            var methodAttributes = MethodAttributes.FamANDAssem | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
            var method = new MethodDefinition(".ctor", methodAttributes, typeDef.Module.TypeSystem.Void) {
                DeclaringType = typeDef
            };
            if (baseConstructorRef != null) {
                method.Body.EmitBaseCallWithDefaultArgumentValues(baseConstructorRef);
            }
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            typeDef.Methods.Add(method);
            Trace.WriteLine(string.Format("Added parameterless constructor to type {0}.", typeDef.FullName), "EnsureParameterlessConstructors");
            return method;
        }

        static void EmitCallBaseConstructor(MethodBody methodBody, MethodReference baseConstructorRef) {
        }


        protected override void ProcessMethod(MethodDefinition methodDef) {
            if (methodDef.IsConstructor && !methodDef.HasParameters && !methodDef.IsStatic) {
                if (methodDef.IsPrivate) {
                    methodDef.IsAssembly = true;
                    Trace.WriteLine(string.Format("Changed {0} from private to internal.", methodDef), "EnsureParameterlessConstructors");
                }
            }
            base.ProcessMethod(methodDef);
        }
    }
}
