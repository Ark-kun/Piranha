using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ark.DotNet {
    public static class PortableHelpers {
        public static List<AssemblyNameReference> GetFrameworkAssemblies(this FrameworkProfile frameworkProfile) {
            var frameworkListXml = XDocument.Load(Path.Combine(frameworkProfile.ReferencesDirectory, "RedistList", "FrameworkList.xml"));
            var assemblies = frameworkListXml.Descendants("File").Select(element => AssemblyNameReference.Parse(element.Attribute("AssemblyName").Value + ", " + string.Join(", ", element.Attributes().Select(a => a.Name + "=" + a.Value)))).ToList();

            if (frameworkProfile.IsPortable) {
                foreach (var assemblyName in assemblies) {
                    assemblyName.IsRetargetable = true;
                    //assemblyName.Culture = null;
                }
            }
            return assemblies;
        }
    }
}
