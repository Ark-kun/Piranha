using System;
using System.Collections.Generic;

namespace Ark.DotNet {
    public class FrameworkInfo {
        public string FrameworkType { get; set; }
        //public FrameworkVersionInfo DefaultVersion { get; set; }
        public Dictionary<Version, FrameworkVersionInfo> Versions { get; set; }
    }
}
