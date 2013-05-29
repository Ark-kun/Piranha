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

        static void DumpAssemblyAndUsageLists(AssemblyDefinition assemblyDef, string fileNameBase, int step) {
            var oldName = assemblyDef.Name.Name;
            assemblyDef.Name.Name += " " + step.ToString();
            assemblyDef.Write(fileNameBase + ".skeleton." + step.ToString() + ".dll");
            assemblyDef.Name.Name = oldName;

            var usedTypes = new HashSet<TypeReference>(GetAllUsedTypes(assemblyDef).Where(t => t != null).Select(t => t.TryResolve() ?? t), TypeReferenceEqualityComparer.Default);
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

        static IEnumerable<TypeReference> GetAllUsedTypes(AssemblyDefinition assemblyDef) {
            foreach (var typeDef in assemblyDef.GetTypesIncludingNested()) {
                foreach (var someTypeDef in GetAllUsedTypes(typeDef)) {
                    yield return someTypeDef;
                }
            }
        }

        static IEnumerable<TypeReference> GetAllUsedTypes(TypeDefinition typeDef) {
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

        static IEnumerable<TypeReference> GetAllUsedTypes(MethodDefinition methodDef) {
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


        public T XXX<T>() {
            return default(T);
        }

        public void XXX2() {
        }
    }

    public class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference> {
        private TypeReferenceEqualityComparer() { }

        static TypeReferenceEqualityComparer _instance = new TypeReferenceEqualityComparer();

        public static TypeReferenceEqualityComparer Default {
            get { return _instance; }
        }

        public bool Equals(TypeReference x, TypeReference y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return x.FullName == y.FullName;
        }

        public int GetHashCode(TypeReference obj) {
            return obj.FullName.GetHashCode();
        }
    }

    public class MethodReferenceEqualityComparer : IEqualityComparer<MethodReference> {
        private MethodReferenceEqualityComparer() { }

        static MethodReferenceEqualityComparer _instance = new MethodReferenceEqualityComparer();

        public static MethodReferenceEqualityComparer Default {
            get { return _instance; }
        }

        public bool Equals(MethodReference x, MethodReference y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return x.FullName == y.FullName;
        }

        public int GetHashCode(MethodReference obj) {
            return obj.FullName.GetHashCode();
        }
    }

    class TestBase {

        public TestBase(bool zzz) {
        }

        public TestBase(string arg, TestBase xxx) {
        }

        public static string Method1(int arg) {
            return "";
        }

        public TestBase This {
            get { return this; }
        }
    }

    class TestDerived : TestBase {

        public TestDerived(bool arg1, int arg2)
            : base(TestBase.Method1(arg2), new TestBase(true).This) {
        }
    }

    class TestBase2 {
        public TestBase2(bool arg) { }
        public TestBase2(int arg) { }
        public TestBase2(long arg) { }
        public TestBase2(TimeSpan arg) { }
        public TestBase2(string arg) { }
        public TestBase2(ref long arg) { }
        public TestBase2(out TimeSpan arg) { arg = default(TimeSpan); }
    }

    class TestDerived2 : TestBase2 {
        public TestDerived2(bool arg) : base(default(bool)) { }
        public TestDerived2(int arg) : base(default(int)) { }
        public TestDerived2(long arg) : base(default(long)) { }
        public TestDerived2(TimeSpan arg) : base(default(TimeSpan)) { }
        public TestDerived2(string arg) : base(default(string)) { }
        public TestDerived2(ref long arg) : base(ref arg) { }
        public TestDerived2(out TimeSpan arg) : base(out arg) { }

        public void TestMethod(ref long arg) { }
        public void TestMethod(out TimeSpan arg) { arg = default(TimeSpan); }
    }

    class TestBase3<T> {
        public TestBase3(T arg) { }
    }

    class TestDerived3a<T> : TestBase3<T> {
        public TestDerived3a(T arg) : base(default(T)) { }
    }

    class TestDerived3b : TestBase3<int> {
        public TestDerived3b() : base(default(int)) { }
    }

    class TestDerived3c : TestBase3<string> {
        public TestDerived3c() : base(default(string)) { }
    }


    public static class CollectionHelpers {
        public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate) {
            var itemsToRemove = collection.Where(predicate).ToList();
            foreach (var item in itemsToRemove) {
                collection.Remove(item);
            }
        }
    }

    public static class CecilHelpers {
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


        public static void EmitDefaultInitializedVariable(this MethodBody body, string name, TypeReference type) {
            body.InitLocals = true;
            var variableDef = new VariableDefinition(name, type);
            body.Variables.Add(variableDef);

            var il = body.GetILProcessor();
            il.Emit(OpCodes.Ldloca, variableDef);
            il.Emit(OpCodes.Initobj, variableDef.VariableType);
            il.Emit(OpCodes.Ldloc, variableDef);
        }
    }
}
