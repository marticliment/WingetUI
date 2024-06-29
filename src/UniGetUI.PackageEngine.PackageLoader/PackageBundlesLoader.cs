using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Classes.Serializable;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.PackageLoader
{
    public class PackageBundlesLoader : AbstractPackageLoader
    {
        public PackageBundlesLoader(IEnumerable<PackageManager> managers)
        : base(managers, "PACKAGE_BUNDLES", AllowMultiplePackageVersions: true)
        {
        }

#pragma warning disable
        protected override async Task<bool> IsPackageValid(IPackage package)
        {
            return true;
        }
#pragma warning restore

        protected override Task<IPackage[]> LoadPackagesFromManager(IPackageManager manager)
        {
            return Task.Run(() => new IPackage[0]);
        }

#pragma warning disable CS1998
        protected override async Task WhenAddingPackage(IPackage package)
        {
            if(package.GetInstalledPackage() != null)
                package.SetTag(PackageTag.AlreadyInstalled);
        }
#pragma warning restore CS1998

        public void AddPackages(IEnumerable<IPackage> packages)
        {
            foreach (var package in packages)
            {
                if(!Contains(package)) AddPackage(package);
            }
            RaisePackagesChangedEvent();
        }

        /*public void AddPackages(IEnumerable<SerializablePackage_v1> packages_data)
        {
            foreach (var package_data in packages_data)
            { 
                IPackage? package = Package.FromSerializable(package_data);
                if (!Contains(package)) AddPackage(package);
            }
            RaisePackagesChangedEvent();
        }

        public void AddPackages(IEnumerable<SerializableIncompatiblePackage_v1> packages_data)
        {
            foreach (var package_data in packages_data)
            {
                InvalidImportedPackage package = Package.FromSerializable(package_data);
                if (!Contains(package)) AddPackage(package);
            }
            RaisePackagesChangedEvent();
        }
        */
    }
}
