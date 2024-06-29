using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Classes.Serializable
{
    public class SerializableUpdatesOptions_v1
    {
        public bool UpdatesIgnored { get; set; } = false;
        public string IgnoredVersion { get; set; } = "";
    }
}
