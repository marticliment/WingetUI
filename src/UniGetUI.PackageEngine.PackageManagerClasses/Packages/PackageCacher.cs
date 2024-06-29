using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Classes.Packages
{
    internal static class PackageCacher
    {
        private static readonly Dictionary<long, IPackage> __available_pkgs = new();
        private static readonly Dictionary<long, IPackage> __upgradable_pkgs = new();
        private static readonly Dictionary<long, IPackage> __installed_pkgs = new();

        /// <summary>
        /// Will check if a given Package is already in the cache. If not, it will be added to it
        /// This checks only the "Discover Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise p</returns>
        public static IPackage GetAvailablePackage(IPackage p)
        {
            IPackage? new_package = GetAvailablePackageOrNull(p);
            if (new_package == null) AddPackageToCache(p, __available_pkgs);
            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache. If not, it will be added to it
        /// This checks only the "Software Updates" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise p</returns>
        public static IPackage GetUpgradablePackage(IPackage p)
        {
            IPackage? new_package = GetUpgradablePackageOrNull(p);
            if (new_package == null) AddPackageToCache(p, __upgradable_pkgs);
            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache. If not, it will be added to it
        /// This checks only the "Installed Packages" cache
        /// </summary>
        /// <param name="p">The package to check</param>
        /// <returns>The already existing package if any, otherwhise p</returns>
        public static IPackage GetInstalledPackage(IPackage p)
        {
            IPackage? new_package = GetInstalledPackageOrNull(p);
            if (new_package == null) AddPackageToCache(p, __installed_pkgs);
            return new_package ?? p;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Discover Packages" cache
        /// </summary>
        /// <param name="other">The package to check</param>
        /// <returns>The already existing package if any, otherwhise null</returns>
        public static IPackage? GetAvailablePackageOrNull(IPackage other)
        {
            if(__available_pkgs.TryGetValue(other.GetHash(), out IPackage? equivalent_package))
            {
                return equivalent_package;
            }
            return null;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Software Updates" cache
        /// </summary>
        /// <param name="other">The package to check</param>
        /// <returns>The already existing package if any, otherwhise null</returns>
        public static IPackage? GetUpgradablePackageOrNull(IPackage other)
        {
            if (__upgradable_pkgs.TryGetValue(other.GetHash(), out IPackage? equivalent_package))
            {
                return equivalent_package;
            }
            return null;
        }

        /// <summary>
        /// Will check if a given Package is already in the cache.
        /// This checks only the "Installed Packages" cache
        /// </summary>
        /// <param name="other">The package to check</param>
        /// <returns>The already existing package if any, otherwhise null</returns>
        public static IPackage? GetInstalledPackageOrNull(IPackage other)
        {
            if (__installed_pkgs.TryGetValue(other.GetVersionedHash(), out IPackage? equivalent_package))
            {
                return equivalent_package;
            }
            return null;
        }

        /// <summary>
        /// Checks wether a Package with a newer version has been found in the Installed Packages cache
        /// </summary>
        /// <param name="other">The package to check agains</param>
        /// <returns>True if a newer version was found, false otherwhise</returns>
        public static bool NewerVersionIsInstalled(IPackage other)
        {
            foreach (IPackage found in __installed_pkgs.Values)
            {
                if (found.IsEquivalentTo(other) && found.VersionAsFloat == other.NewVersionAsFloat)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddPackageToCache(IPackage package, Dictionary<long, IPackage> map)
        {
            long hash = map == __installed_pkgs ? package.GetVersionedHash() : package.GetHash();
            map.Add(hash, package);
        }
    }
}
