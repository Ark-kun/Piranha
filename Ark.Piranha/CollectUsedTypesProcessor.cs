using Ark.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ark.Piranha {
    public class CollectUsedTypesProcessor : CecilProcessor {
        HashSet<TypeReference> _usedTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);

        public ISet<TypeReference> UsedTypes {
            get { return _usedTypes; }
        }

        public void DumpToFile(string fileName) {
            var usedTypes = new HashSet<TypeReference>(UsedTypes.Where(t => t != null).Select(t => t.TryResolve() ?? t), TypeReferenceEqualityComparer.Default);
            using (var usedTypesWriter = File.CreateText(fileName)) {
                foreach (string fullTypeName in usedTypes.Select(typeRef => "[" + (typeRef.Module == null ? "?" : typeRef.Module.Assembly.Name.Name) + "]" + typeRef.FullName).OrderBy(tn => tn).Distinct()) {
                    usedTypesWriter.WriteLine(fullTypeName);
                }
            }
        }

        public override void ProcessType(TypeDefinition typeDef) {
            _usedTypes.Add(typeDef);
            if (typeDef.BaseType != null) {
                _usedTypes.Add(typeDef.BaseType);
            }
            base.ProcessType(typeDef);
        }

        public override void ProcessField(FieldDefinition fieldDef) {
            _usedTypes.Add(fieldDef.FieldType);
            base.ProcessField(fieldDef);
        }

        public override void ProcessMethod(MethodDefinition methodDef) {
            _usedTypes.Add(methodDef.ReturnType);
            foreach (var parameter in methodDef.Parameters) {
                if (parameter.ParameterType.IsGenericParameter) {
                    _usedTypes.Add(parameter.ParameterType);
                }
            }
            if (methodDef.HasBody) {
                var body = methodDef.Body;
                foreach (var variable in body.Variables) {
                    _usedTypes.Add(variable.VariableType);
                }
                foreach (var instruction in body.Instructions) {
                    if (instruction.OpCode == OpCodes.Newobj) {
                        var newObjTypeRef = ((MemberReference)instruction.Operand).DeclaringType;
                        _usedTypes.Add(newObjTypeRef);
                    }
                    if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt) {
                        var callMethodRef = instruction.Operand as MethodReference;
                        _usedTypes.Add(callMethodRef.DeclaringType);
                    }
                }
            }
            base.ProcessMethod(methodDef);
        }
    }
}
