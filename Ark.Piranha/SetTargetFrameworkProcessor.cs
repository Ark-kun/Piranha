using Ark.Cecil;
using Ark.DotNet;
using Ark.Linq;
using Mono.Cecil;
using System;
using System.Runtime.Versioning;

namespace Ark.Piranha {
    public class SetTargetFrameworkProcessor : CecilProcessor {
        string _frameworkProfile;

        public SetTargetFrameworkProcessor(string frameworkProfile) {
            _frameworkProfile = frameworkProfile;
        }

        public SetTargetFrameworkProcessor(FrameworkProfile frameworkProfile)
            : this(frameworkProfile.FullName) {
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            MethodReference attributeConstructor = assemblyDef.MainModule.Import(typeof(TargetFrameworkAttribute).GetConstructor(new Type[] { typeof(string) }));
            var targetFrameworkAttribute = new CustomAttribute(attributeConstructor);
            targetFrameworkAttribute.ConstructorArguments.Add(new CustomAttributeArgument(assemblyDef.MainModule.TypeSystem.String, _frameworkProfile));

            assemblyDef.CustomAttributes.RemoveWhere(attr => attr.AttributeType.IsEqualTo(targetFrameworkAttribute.AttributeType));
            assemblyDef.CustomAttributes.Add(targetFrameworkAttribute);

            base.ProcessAssembly(assemblyDef);
        }
    }
}
