using UniGetUI.Core.IconEngine;

namespace UniGetUI.PackageEngine.Interfaces.ManagerProviders
{
    public interface IPackageDetailsProvider
    {
        /// <summary>
        /// Returns a PackageDetails object that represents the details for the given Package object.
        /// This method is fail-safe and will return a valid but empty PackageDetails object with the package 
        /// id if an error occurs.
        /// </summary>
        /// <param name="details">The PackageDetails instance to load</param>
        /// <returns>A PackageDetails object</returns>
        public abstract Task GetPackageDetails(IPackageDetails details);


        /// <summary>
        /// Returns the available versions to install for the given package. 
        /// If the manager does not support listing the versions, an empty array will be returned.
        /// This method is fail-safe and will return an empty array if an error occurs.
        /// </summary>
        /// <param name="package">The package from which to load its versions</param>
        /// <returns>An array of stings containing the found versions, an empty array if none.</returns>
        public abstract Task<string[]> GetPackageVersions(IPackage package);

        /// <summary>
        /// Returns an Uri pointing to the icon of this package. 
        /// The uri may be either a ms-appx:/// url or a http(s):// protocol url
        /// </summary>
        /// <param name="package">The package from which to load the icon</param>
        /// <returns>A full path to a valid icon file</returns>
        public abstract Task<CacheableIcon?> GetPackageIconUrl(IPackage package);


        /// <summary>
        /// Returns the URLs to the screenshots (if any) of this package. 
        /// </summary>
        /// <param name="package">The package from which to load the screenshots</param>
        /// <returns>An array with valid URIs to the screenshots</returns>
        public abstract Task<Uri[]> GetPackageScreenshotsUrl(IPackage package);
    }
}
