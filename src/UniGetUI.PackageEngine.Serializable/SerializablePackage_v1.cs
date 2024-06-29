using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.Classes.Serializable
{
    public class SerializablePackage_v1
    {
        /// <summary>
        /// The package full, valid identifier
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// The package display name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The installed version of the package
        /// </summary>
        public string Version { get; set; } = "";

        /// <summary>
        /// The name of the source, WITHOUT the Package Manager name
        /// </summary>
        public string Source { get; set; } = "";

        /// <summary>
        /// The name of the package manager
        /// </summary>
        public string ManagerName { get; set; } = "";

        /// <summary>
        /// The InstallationOptions associated to this package
        /// </summary>
        public SerializableInstallationOptions_v1 InstallationOptions { get; set; } = new();

        /// <summary>
        /// The Updates preferences associated to this package
        /// </summary>
        public SerializableUpdatesOptions_v1 Updates { get; set; } = new();

        /// <summary>
        /// Returns an equivalent copy of the current package as an Invalid Serializable Package.
        /// The reverse operation is not possible, since data is lost.
        /// </summary>
        /// <returns></returns>
        public SerializableIncompatiblePackage_v1 GetInvalidEquivalent()
        {
            return new SerializableIncompatiblePackage_v1()
            {
                Id = Id,
                Name = Name,
                Version = Version,
                Source = Source,
            };
        }
    }
}
