using System;
using System.Diagnostics;
using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Piranha {
    public class RetargetReferencesProcessor : CecilProcessor {
        IDictionary<string, AssemblyNameReference> _assemblyReplacements;
        bool _removeOthers;

        public RetargetReferencesProcessor(FrameworkProfile frameworkProfile, bool removeOthers = false)
            : this(frameworkProfile.GetFrameworkAssemblies(), removeOthers) {
        }

        public RetargetReferencesProcessor(IEnumerable<AssemblyNameReference> assemblyReplacements, bool removeOthers = false)
            : this(assemblyReplacements.ToDictionary(asm => asm.Name, asm => asm), removeOthers) {
        }

        public RetargetReferencesProcessor(IDictionary<string, AssemblyNameReference> assemblyReplacements, bool removeOthers = false) {
            _assemblyReplacements = assemblyReplacements;
            _removeOthers = removeOthers;
        }

        protected override void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            for (int i = assemblyNameRefs.Count - 1; i >= 0; --i) {
                AssemblyNameReference replacement = null;
                var other = assemblyNameRefs[i];
                var otherName = other.Name + "," + other.Version;
                if (_assemblyReplacements.TryGetValue(other.Name, out replacement)) {                
                    assemblyNameRefs[i].Version = replacement.Version;
                    assemblyNameRefs[i].PublicKeyToken = replacement.PublicKeyToken;
                    assemblyNameRefs[i].IsRetargetable = replacement.IsRetargetable;
                    var replacementName = replacement.Name + "," + replacement.Version;
                    Trace.WriteLine(string.Format("Replacing {0} with {1}.", otherName, replacementName), "RetargetReferences");
                } else {
                    if (_removeOthers) {
                        try { 
                            var otherAss = moduleDef.AssemblyResolver.Resolve(other);
                            var otherProfile = otherAss.GetAssemblyProfileFromAttribute();
                            if (otherProfile != null && otherProfile.IsPortable) {
                                Trace.WriteLine(string.Format("Keeping {0}.", otherName), "RetargetReferences");
                                continue;
                            }
                        } catch(Exception ex) {
                            Trace.WriteLine(string.Format("Failed resolving {0}.", otherName), "RetargetReferences");
                        }
                        Trace.WriteLine(string.Format("Removing {0}.", otherName), "RetargetReferences");
                        assemblyNameRefs.RemoveAt(i);
                    } else {
                        Trace.WriteLine(string.Format("Keeping {0}.", otherName), "RetargetReferences");
                    }
                }
            }
            base.ProcessAssemblyReferences(moduleDef, assemblyNameRefs);
        }
    }
}
