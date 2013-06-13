using System;
using System.Collections.Generic;

namespace Ark.DotNet {
    public class FrameworkVersionInfo {
        public FrameworkInfo FrameworkInfo { get; set; } //parent

        public Version Version { get; set; }
        public FrameworkProfileInfo DefaultProfile { get; set; }
        public Dictionary<string, FrameworkProfileInfo> Profiles { get; set; }
    }
}
