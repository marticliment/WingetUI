using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using UniGetUI.Core.Data;
using UniGetUI.Core.IconEngine;
using UniGetUI.Core.Logging;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Manager;
using UniGetUI.PackageEngine.Classes.Packages;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Serializable;

namespace UniGetUI.PackageEngine.PackageClasses
{
    public class ImportedPackage : Package
    {
        /// <summary>
        /// Constuct an invalid package with a given name, id, version, source and manager, and an optional scope.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        /// <param name="version"></param>
        /// <param name="source"></param>
        SerializableUpdatesOptions_v1 updates_options;
        SerializableInstallationOptions_v1 installation_options;

        public ImportedPackage(SerializablePackage_v1 raw_data, IPackageManager manager, IManagerSource source)
            : base(raw_data.Name, raw_data.Id, raw_data.Version, source, manager)
        {
            installation_options = raw_data.InstallationOptions;
            updates_options = raw_data.Updates;
        }

        public async Task RegisterPackage()
        {
            var options = InstallationOptions.FromSerialized(installation_options, this);
            await options.SaveToDiskAsync();

            if (updates_options.UpdatesIgnored)
            {
                await AddToIgnoredUpdatesAsync(updates_options.IgnoredVersion);
            }
        }
    }
}
