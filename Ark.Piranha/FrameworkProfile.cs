using System;
using System.IO;

namespace Ark.DotNet {
    public class FrameworkProfile {
        string _name;
        string _version;
        string _profile;

        public FrameworkProfile(string name, string version, string profile = null) {
            _name = name;
            _version = version;
            _profile = profile;
        }

        public string Name {
            get { return _name; }
        }

        public string Version {
            get { return _version; }
        }

        public string Profile {
            get { return _profile; }
        }

        public string FullName {
            get {
                var fullName = Name;
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
                return Name == _netPortable;
            }
        }

        public bool IsClientProfile {
            get {
                return Profile == _profileClient;
            }
        }

        public static string ReferenceAssembliesDirectory {
            get {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Reference Assemblies", "Microsoft", "Framework");
            }
        }

        public string ReferencesDirectory {
            get {
                string directory = ReferenceAssembliesDirectory;
                if (Name != null) {
                    directory = Path.Combine(directory, Name);
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

        static string _netFramework = ".NETFramework";
        static string _netPortable = ".NETPortable";
        static string _netMicroFramework = ".NETMicroFramework";
        static string _netCore = ".NETCore";
        static string _monoAndroid = "MonoAndroid";
        static string _monoTouch = "MonoTouch";
        static string _silverlight = "Silverlight";
        static string _windowsPhone = "WindowsPhone";

        static string _version40 = "v4.0";
        static string _version45 = "v4.5";

        static string _profileClient = "Client";

        static FrameworkProfile _netFramework40 = new FrameworkProfile(_netFramework, _version40);
        static FrameworkProfile _netFramework40Client = new FrameworkProfile(_netFramework, _version40, _profileClient);
        static FrameworkProfile _netFramework45 = new FrameworkProfile(_netFramework, _version45);
        static FrameworkProfile _netPortable_NET40_SL4_WP71_Windows8 = new FrameworkProfile(_netPortable, _version40, "Profile88");
        static FrameworkProfile _netPortable_NET45_Windows8 = new FrameworkProfile(_netPortable, _version45, "Profile7");

        public static FrameworkProfile NetFramework40 { get { return _netFramework40; } }

        public static FrameworkProfile NetFramework40Client { get { return _netFramework40Client; } }

        public static FrameworkProfile NetFramework45 { get { return _netFramework45; } }

        public static FrameworkProfile NetPortable_NET40_SL4_WP71_Windows8 { get { return _netPortable_NET40_SL4_WP71_Windows8; } }

        public static FrameworkProfile NetPortable_NET45_Windows8 { get { return _netPortable_NET45_Windows8; } }
    }
}
