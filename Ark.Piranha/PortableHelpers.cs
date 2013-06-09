using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ark.DotNet {
    public static class PortableHelpers {
        public static List<AssemblyNameReference> GetFrameworkAssemblies(this FrameworkProfile frameworkProfile) {
            var frameworkListXml = XDocument.Load(Path.Combine(frameworkProfile.ReferencesDirectory, "RedistList", "FrameworkList.xml"));
            var assemblies = frameworkListXml.Descendants("File").Select(element => AssemblyNameReference.Parse(element.Attribute("AssemblyName").Value + ", " + string.Join(", ", element.Attributes().Select(a => a.Name + "=" + a.Value)))).ToList();
            
            foreach (var assemblyName in assemblies) {
                if (assemblyName.Culture.Equals("neutral", StringComparison.InvariantCultureIgnoreCase)) {
                    assemblyName.Culture = null;
                }
                assemblyName.IsRetargetable = frameworkProfile.IsPortable;
            }
            return assemblies;
        }

        public static FrameworkProfile GetAssemblyProfileFromAttribute(this AssemblyDefinition assemblyDef) {
            var targetFrameworkAttribute = assemblyDef.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
            if (targetFrameworkAttribute != null) {
                var frameworkName = (string)targetFrameworkAttribute.ConstructorArguments.First().Value;
                return FrameworkProfile.Parse(frameworkName);
            }
            return null;
        }

        public static FrameworkProfile GuessAssemblyProfile(this AssemblyDefinition assemblyDef) {
            var targetFramework = GetAssemblyProfileFromAttribute(assemblyDef);
            if(targetFramework != null) {
                return targetFramework;
            }

            var mscorlibVersions = assemblyDef.Modules.SelectMany(moduleDef => moduleDef.AssemblyReferences).Where(assemblyName => assemblyName.Name == "mscrolib").Select(assemblyName => assemblyName.Version).Distinct().ToList();
            if (!mscorlibVersions.Any()) {
                Trace.WriteLine(string.Format("Strange: Assembly {0} doesn't reference mscorlib.dll.", assemblyDef), "GetAssemblyProfile");
                throw new Exception("The assembly doesn't reference mscorlib.");
            }
            if (mscorlibVersions.Count > 1) {
                Trace.WriteLine(string.Format("Strange: Assembly {0} references multiple versions of mscorlib.dll: {1}.", assemblyDef, string.Join(", ", mscorlibVersions)), "GetAssemblyProfile");
            }

            var mscorlibVersion =  mscorlibVersions.First();
            string version = "v" + mscorlibVersion.Major + "." + mscorlibVersion.Minor;
            return new FrameworkProfile(FrameworkProfile.Frameworks.NetFramework, version);
        }
    }
}
