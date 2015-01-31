using Ark.Cecil;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace Ark.DotNet {
    public class FrameworkProfileInfo {
        public FrameworkVersionInfo FrameworkVersionInfo { get; set; } //parent

        public string Description { get; set; }
        public string Profile { get; set; }
        public string Directory { get; set; }

        public HashSet<AssemblyNameReference> Assemblies { get; set; }

        public static FrameworkProfileInfo Parse(XDocument frameworkList) {
            var profileInfo = new FrameworkProfileInfo();
            var nameAttr = frameworkList.Root.Attribute("Name");
            profileInfo.Description = (nameAttr != null ? nameAttr.Value : null);
            profileInfo.Assemblies = new HashSet<AssemblyNameReference>(CecilEqualityComparer.Default);

            //frameworkList.Descendants("File").Select(file => AssemblyNameReference.Parse(string.Format("{0}, Version={1}, ",file.Attribute("AssemblyName").Value,)   ))
            foreach (var fileElement in frameworkList.Descendants("File")) {
                var assemblyName = fileElement.Attribute("AssemblyName").Value;

                var versionString = fileElement.Attribute("Version").Value;
                Version version = null;
                if (!Version.TryParse(versionString, out version)) {
                    Trace.WriteLine(string.Format("Warning: Couldn't parse version of the {0} assembly description.", fileElement.ToString()));
                }


                string culture = null;
                var attrib = fileElement.Attribute ("Culture");
                if (attrib != null && attrib.Value != "neutral") {
                    culture = attrib.Value;
                }

                var publicKeyTokenString = fileElement.Attribute("PublicKeyToken").Value;
                var publicKeyToken = new byte[publicKeyTokenString.Length / 2];
                for (int j = 0; j < publicKeyToken.Length; j++) {
                    publicKeyToken[j] = Byte.Parse(publicKeyTokenString.Substring(j * 2, 2), NumberStyles.HexNumber);
                }
                var assemblyNameRef = new AssemblyNameReference(assemblyName, version) { Culture = culture, PublicKeyToken = publicKeyToken };
                profileInfo.Assemblies.Add(assemblyNameRef);
            }

            return profileInfo;
        }
    }
}
