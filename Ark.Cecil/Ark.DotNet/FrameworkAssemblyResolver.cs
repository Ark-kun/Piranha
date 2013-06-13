using Ark.Cecil;
using Ark.DotNet;
using Ark.Linq;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Ark.Piranha {
    public class FrameworkAssemblyResolver : DefaultAssemblyResolver {
        Dictionary<string, FrameworkInfo> _frameworks;
        List<Tuple<AssemblyNameReference, FrameworkProfileInfo>> _assemblyIndex;
        List<Tuple<AssemblyNameReference, FrameworkProfileInfo>> _defaultProfileAssemblyIndex;

        public FrameworkAssemblyResolver() {
            BuildReferenceAssemblyIndex();
        }

        public Dictionary<string, FrameworkInfo> Frameworks {
            get { return _frameworks; }
        }

        public List<Tuple<AssemblyNameReference, FrameworkProfileInfo>> AssemblyIndex {
            get { return _assemblyIndex; }
        }

        public List<Tuple<AssemblyNameReference, FrameworkProfileInfo>> DefaultProfileAssemblyIndex {
            get { return _defaultProfileAssemblyIndex; }
        }

        public void BuildReferenceAssemblyIndex() {
            var frameworkTypeDirs = Directory.GetDirectories(FrameworkProfile.Paths.ReferenceAssembliesDirectory).ToList();

            var frameworks = new Dictionary<string, FrameworkInfo>();
            var assemblyIndex = new List<Tuple<AssemblyNameReference, FrameworkProfileInfo>>();
            var defaultProfileAssemblyIndex = new List<Tuple<AssemblyNameReference, FrameworkProfileInfo>>();

            var legacyVersions = frameworkTypeDirs.Exclude(dir => { dir = Path.GetFileName(dir); return dir == FrameworkProfile.Versions.v30 || dir == FrameworkProfile.Versions.v35; }).ToArray();

            foreach (var frameworkTypeDir in frameworkTypeDirs) {
                var frameworkType = Path.GetFileName(frameworkTypeDir);
                var frameworkVersionDirs = Directory.GetDirectories(frameworkTypeDir).ToList();

                if (frameworkType == FrameworkProfile.Frameworks.NetFramework) {
                    frameworkVersionDirs.AddRange(legacyVersions);
                }

                var frameworkInfo = new FrameworkInfo() { FrameworkType = frameworkType };

                foreach (var versionDir in frameworkVersionDirs) {
                    var versionString = Path.GetFileName(versionDir);
                    Version version = null;
                    if (versionString.StartsWith("v") && Version.TryParse(versionString.TrimStart('v'), out version)) {
                        if (frameworkInfo.Versions == null) {
                            frameworkInfo.Versions = new Dictionary<Version, FrameworkVersionInfo>();
                        }
                        FrameworkVersionInfo versionInfo;
                        if (!frameworkInfo.Versions.TryGetValue(version, out versionInfo)) {
                            versionInfo = new FrameworkVersionInfo() {
                                FrameworkInfo = frameworkInfo,
                                Version = version,
                                Profiles = new Dictionary<string, FrameworkProfileInfo>()
                            };
                            frameworkInfo.Versions.Add(version, versionInfo);
                        }

                        var assemblyListFile = Path.Combine(versionDir, FrameworkProfile.Paths.FrameworkListFile);
                        if (File.Exists(assemblyListFile)) {
                            //default profile
                            var profileInfo = FrameworkProfileInfo.Parse(XDocument.Load(assemblyListFile));
                            profileInfo.Profile = string.Empty;
                            profileInfo.Directory = versionDir;
                            profileInfo.FrameworkVersionInfo = versionInfo;

                            if (versionInfo.DefaultProfile == null) {
                                versionInfo.DefaultProfile = profileInfo;
                            } else {
                                Trace.WriteLine(string.Format("Warning: Found duplicate default profiles: {0} in {1} and {2} in {3}.", versionInfo.DefaultProfile.Profile, versionInfo.DefaultProfile.Directory, profileInfo.Profile, profileInfo.Directory));
                            }

                            versionInfo.Profiles.Add(profileInfo.Profile, profileInfo);

                            foreach (var assemblyNameRef in profileInfo.Assemblies) {
                                assemblyIndex.Add(Tuple.Create(assemblyNameRef, profileInfo));
                                defaultProfileAssemblyIndex.Add(Tuple.Create(assemblyNameRef, profileInfo));
                            }
                        }
                        var profilesDirectory = Path.Combine(versionDir, FrameworkProfile.Paths.ProfilesSubdirectory);
                        if (Directory.Exists(profilesDirectory)) {
                            string[] frameworkProfileDirs = Directory.GetDirectories(profilesDirectory).ToArray();
                            foreach (var frameworkProfileDir in frameworkProfileDirs) {
                                var frameworkProfileString = Path.GetFileName(frameworkProfileDir);
                                var profileAssemblyListFile = Path.Combine(frameworkProfileDir, FrameworkProfile.Paths.FrameworkListFile);
                                if (File.Exists(profileAssemblyListFile)) {
                                    var profileInfo = FrameworkProfileInfo.Parse(XDocument.Load(profileAssemblyListFile));
                                    profileInfo.Profile = frameworkProfileString;
                                    profileInfo.Directory = frameworkProfileDir;
                                    profileInfo.FrameworkVersionInfo = versionInfo;

                                    if (!versionInfo.Profiles.ContainsKey(profileInfo.Profile)) {
                                        versionInfo.Profiles.Add(profileInfo.Profile, profileInfo);
                                    } else {
                                        Trace.WriteLine(string.Format("Warning: Found profiles with duplicate identifier {0}: in {1} and {2}.", profileInfo.Profile, versionInfo.Profiles[profileInfo.Profile].Directory, profileInfo.Directory));
                                    }

                                    foreach (var assemblyNameRef in profileInfo.Assemblies) {
                                        assemblyIndex.Add(Tuple.Create(assemblyNameRef, profileInfo));
                                    }
                                }
                            }
                        }
                        if (!(versionInfo.DefaultProfile != null || (versionInfo.Profiles != null && versionInfo.Profiles.Any()))) {
                            frameworkInfo.Versions.Remove(version);
                        }
                    } else {
                        Trace.WriteLine(string.Format("Strange: Cannot parse the version of the framework in {0}. Skipping.", versionDir));
                    }
                }
                if (frameworkInfo.Versions != null && frameworkInfo.Versions.Any()) {
                    frameworks.Add(frameworkInfo.FrameworkType, frameworkInfo);
                }
            }

            //var mscorlibs = defaultProfileAssemblyIndex.Where(kv => kv.Item1.Name == "mscorlib").ToList();
            _frameworks = frameworks;
            _assemblyIndex = assemblyIndex;
            _defaultProfileAssemblyIndex = defaultProfileAssemblyIndex;
        }

        public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters) {
            var res = base.Resolve(name, parameters);
            if (res == null || res.FullName != name.FullName) {
                var defaultProfiles = _defaultProfileAssemblyIndex.Where(kv => CecilEqualityComparer.AreEqual(kv.Item1, name)).ToList();
                if (defaultProfiles.Any()) {
                    var profile = defaultProfiles.First().Item2;
                    var assemblyFile = Path.Combine(profile.Directory, name.Name + ".dll");
                    if (File.Exists(assemblyFile)) {
                        Trace.WriteLine(string.Format("Successfully resolved assembly {0} to profile {1}.", name, profile.Directory), "ReferenceSearchingMetadataResolver");
                        return ModuleDefinition.ReadModule(assemblyFile, parameters).Assembly;
                    } else {
                        throw new Exception(string.Format("Assembly {0} doesn't exist.", assemblyFile));
                    }
                }
            }
            return res;
        }
    }
}
