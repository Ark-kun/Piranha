using Ark.Cecil;
using Ark.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piranha {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("Usage: piranha.exe <library>");
                Environment.Exit(1);
            }
            string inputFileName = args[0];
            //string inputFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string outputFileBase = Path.GetFileNameWithoutExtension(inputFileName);
            string outputFileName = outputFileBase + ".skeleton.dll";

            var inputStream = File.OpenRead(inputFileName);
            var outputStream = File.Create(outputFileName);

            var assemblyDef = AssemblyDefinition.ReadAssembly(inputStream, new ReaderParameters() { ReadSymbols = true });
            //assemblyDef.Name.Name += " (Skeleton)";

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 0);

            var usedConstructors = new HashSet<MethodReference>(MethodReferenceEqualityComparer.Default);

            //Step 1: Removing all bodies.
            foreach (var typeDef in assemblyDef.GetTypesIncludingNested()) {
                var moduleDef = typeDef.Module;
                var typeSystem = moduleDef.TypeSystem;
                var voidTypeDef = typeSystem.Void;
                var valueTypeTypeRef = new TypeReference("System", "ValueType", moduleDef, typeSystem.Corlib);

                foreach (var methodDef in typeDef.Methods) {
                    Console.WriteLine(methodDef.FullName);
                    if (methodDef.HasBody) {
                        var body = methodDef.Body;

                        if (methodDef.IsConstructor && !methodDef.IsStatic && !typeDef.IsValueType) {
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
                                body.ExceptionHandlers.Clear();
                                body.Variables.Clear();
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
                                usedConstructors.Add(constructorRef); //Workaround to prevent removal of used internal constructors.
                            } else {
                                Debug.WriteLine(string.Format("Strange: Constructor {0} doesn't call base type ({1}) constructor.", methodDef, methodDef.DeclaringType.BaseType));
                            }
                            body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                        } else {
                            body.Instructions.Clear();
                            body.ExceptionHandlers.Clear();
                            body.Variables.Clear();

                            var il = body.GetILProcessor();
                            if (methodDef.ReturnType != voidTypeDef) {
                                body.InitLocals = true;
                                var variableDef = new VariableDefinition("result", methodDef.ReturnType);
                                body.Variables.Add(variableDef);

                                il.Emit(OpCodes.Ldloca, variableDef);
                                il.Emit(OpCodes.Initobj, variableDef.VariableType);
                                il.Emit(OpCodes.Ldloc, variableDef);
                            }
                            il.Emit(OpCodes.Ret);
                        }
                    }
                }
            }

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 1);

            //Step 2: Removing all private fields
            foreach (var typeDef in assemblyDef.GetTypesIncludingNested()) {
                typeDef.Fields.RemoveWhere(fieldDef => !fieldDef.IsPublic && !fieldDef.IsFamily);
            }

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 2);

            //Step 3: Removing all private members
            foreach (var typeDef in assemblyDef.GetTypesIncludingNested()) {
                //typeDef.Methods.RemoveWhere(methodDef => !methodDef.IsPublic && !methodDef.IsFamily);

                typeDef.Methods.RemoveWhere(methodDef => !methodDef.IsPublic && !methodDef.IsFamily && !(methodDef.IsConstructor && usedConstructors.Contains(methodDef)));
                foreach (var propertyDef in typeDef.Properties.ToList()) {
                    if (propertyDef.GetMethod != null && propertyDef.GetMethod.Module == null) {
                        propertyDef.GetMethod = null;
                    }
                    if (propertyDef.SetMethod != null && propertyDef.SetMethod.Module == null) {
                        propertyDef.SetMethod = null;
                    }
                    if (propertyDef.GetMethod == null && propertyDef.SetMethod == null) {
                        typeDef.Properties.Remove(propertyDef);
                    }
                }
            }

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 3);

            //Step 4: Removing all private types            
            foreach (var typeDef in assemblyDef.GetTypesIncludingNested().ToList()) {
                if (!typeDef.IsPublic) {
                    if (typeDef.IsNested) {
                        typeDef.DeclaringType.NestedTypes.Remove(typeDef);
                    } else {
                        typeDef.Module.Types.Remove(typeDef);
                    }
                }
            }

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 4);

            assemblyDef.Write(outputStream);
        }

        static void DumpAssemblyAndUsageLists(AssemblyDefinition assemblyDef, string fileNameBase, int step) {
            var oldName = assemblyDef.Name.Name;
            assemblyDef.Name.Name += " " + step.ToString();
            assemblyDef.Write(fileNameBase + ".skeleton." + step.ToString() + ".dll");
            assemblyDef.Name.Name = oldName;

            var usedTypes = new HashSet<TypeReference>(assemblyDef.GetAllUsedTypes().Where(t => t != null).Select(t => t.TryResolve() ?? t), TypeReferenceEqualityComparer.Default);
            using (var usedTypesWriter = File.CreateText(fileNameBase + ".usedTypes." + step.ToString() + ".txt")) {
                foreach (string fullTypeName in usedTypes.Select(typeRef => "[" + (typeRef.Module == null ? "?" : typeRef.Module.Assembly.Name.Name) + "]" + typeRef.FullName).OrderBy(tn => tn).Distinct()) {
                    usedTypesWriter.WriteLine(fullTypeName);
                }
            }
            var usedAssemblies = new HashSet<AssemblyDefinition>(usedTypes.Where(typeRef => typeRef.Module != null).Select(typeRef => typeRef.Module.Assembly));
            using (var usedAssembliesWriter = File.CreateText(fileNameBase + ".usedAssemblies." + step.ToString() + ".txt")) {
                foreach (var assemblyName in usedAssemblies.Select(a => a.Name.ToString()).OrderBy(_ => _)) {
                    usedAssembliesWriter.WriteLine(assemblyName);
                }
            }
        }
    }
}
