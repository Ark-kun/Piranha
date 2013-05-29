using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using System;
using System.Collections.Generic;
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

        public override void ProcessCustomAssemblyAttributes(AssemblyDefinition assemblyDef, IList<CustomAttribute> attributes) {
            MethodReference attributeConstructor = assemblyDef.MainModule.Import(typeof(TargetFrameworkAttribute).GetConstructor(new Type[] { typeof(string) }));
            var targetFrameworkAttribute = new CustomAttribute(attributeConstructor);
            targetFrameworkAttribute.ConstructorArguments.Add(new CustomAttributeArgument(assemblyDef.MainModule.TypeSystem.String, _frameworkProfile));

            for (int i = attributes.Count - 1; i >= 0; --i) {
                if (attributes[i].AttributeType.IsEqualTo(targetFrameworkAttribute.AttributeType)) {
                    attributes.RemoveAt(i);
                }
            }
            attributes.Add(targetFrameworkAttribute);
            base.ProcessCustomAssemblyAttributes(assemblyDef, attributes);
        }
    }
}
