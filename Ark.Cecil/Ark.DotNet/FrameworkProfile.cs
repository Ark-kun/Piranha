using System;
using System.IO;

namespace Ark.DotNet {
    public class FrameworkProfile {
        string _frameworkName;
        string _version;
        string _profile;

        public FrameworkProfile(string frameworkName, string version, string profile = null) {
            _frameworkName = frameworkName;
            _version = version;
            _profile = profile;
        }

        public string FrameworkName {
            get { return _frameworkName; }
        }

        public string Version {
            get { return _version; }
        }

        public string Profile {
            get { return _profile; }
        }

        public string FullName {
            get {
                var fullName = FrameworkName;
                if (Version != null) {
                    fullName += ",Version=" + Version;
                }
                if (Profile != null) {
                    fullName += ",Profile=" + Profile;
                }
                return fullName;
            }
        }

        public bool IsPortable {
            get {
                return FrameworkName == Frameworks.NetPortable;
            }
        }

        public bool IsClientProfile {
            get {
                return Profile == Profiles.Client;
            }
        }

        public static class Paths {
            public static string ReferenceAssembliesDirectory {
                get {
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                        return Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework");
                    else {
                        if (Directory.Exists ("/Applications") && Directory.Exists ("/Library/Frameworks"))
                            return Path.Combine ("/Library/Frameworks/Mono.framework/Versions/Current/lib/mono", "xbuild-frameworks");
                        else
                            return Path.Combine ("/usr/lib/mono", "xbuild-frameworks");
                    }
                }
            }

            public static string FrameworkListFile {
                get {
                    return Path.Combine("RedistList", "FrameworkList.xml");
                }
            }

            public const string ProfilesSubdirectory = "Profile";
        }

        public string ReferencesDirectory {
            get {
                string directory = Paths.ReferenceAssembliesDirectory;
                if (FrameworkName != null) {
                    directory = Path.Combine(directory, FrameworkName);
                }
                if (Version != null) {
                    directory = Path.Combine(directory, Version);
                }
                if (Profile != null) {
                    directory = Path.Combine(directory, "Profile", Profile);
                }
                return directory;
            }
        }

        public static FrameworkProfile Parse(string frameworkName) {
            var parts = frameworkName.Split(',');
            if (parts.Length < 2) {
                throw new ArgumentException("Not enough framework name parts.");
            }
            if (parts.Length > 3) {
                throw new ArgumentException("Too many framework name parts.");
            }
            string name = parts[0];
            string version = null;
            string profile = null;

            for(int i = 1; i <parts.Length; ++i) {
                var keyValue = parts[i].Split('=');
                string key = keyValue[0];
                string value = keyValue[1];
                if (key.Equals("Version", StringComparison.OrdinalIgnoreCase)) {
                    version = value;
                }
                if (key.Equals("Profile", StringComparison.OrdinalIgnoreCase)) {
                    profile = value;
                }
            }
            return new FrameworkProfile(name, version, profile);
        }

        public static class Frameworks {
            public const string NetFramework = ".NETFramework";
            public const string NetPortable = ".NETPortable";
            public const string NetMicroFramework = ".NETMicroFramework";
            public const string NetCore = ".NETCore";
            public const string MonoAndroid = "MonoAndroid";
            public const string MonoTouch = "MonoTouch";
            public const string Silverlight = "Silverlight";
            public const string WindowsPhone = "WindowsPhone";
        }

        public static class Versions {
            public const string v20 = "v2.0";
            public const string v30 = "v3.0";
            public const string v35 = "v3.5";
            public const string v40 = "v4.0";
            public const string v45 = "v4.5";
        }

        public static class Profiles {
            public const string Client = "Client";
        }

        static FrameworkProfile _netFramework40 = new FrameworkProfile(Frameworks.NetFramework, Versions.v40);
        static FrameworkProfile _netFramework40Client = new FrameworkProfile(Frameworks.NetFramework, Versions.v40, Profiles.Client);
        static FrameworkProfile _netFramework45 = new FrameworkProfile(Frameworks.NetFramework, Versions.v45);
        static FrameworkProfile _netPortable_NET40_SL4_WP71_Windows8 = new FrameworkProfile(Frameworks.NetPortable, Versions.v40, "Profile88");
        static FrameworkProfile _netPortable_NET45_Windows8 = new FrameworkProfile(Frameworks.NetPortable, Versions.v45, "Profile7");

        public static FrameworkProfile NetFramework40 { get { return _netFramework40; } }

        public static FrameworkProfile NetFramework40Client { get { return _netFramework40Client; } }

        public static FrameworkProfile NetFramework45 { get { return _netFramework45; } }

        public static FrameworkProfile NetPortable_NET40_SL4_WP71_Windows8 { get { return _netPortable_NET40_SL4_WP71_Windows8; } }

        public static FrameworkProfile NetPortable_NET45_Windows8 { get { return _netPortable_NET45_Windows8; } }
    }
}
