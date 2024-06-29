using UniGetUI.PackageEngine.ManagerClasses.Manager;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class ManagerSource : IManagerSource
    {
        public virtual string IconId { get { return Manager.Properties.IconId; } }
        public bool IsVirtualManager { get; }

        public IPackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; set; }
        public int? PackageCount { get; }
        public string UpdateDate { get; }

        public ManagerSource(IPackageManager manager, string name, Uri url, int? packageCount = 0, string updateDate = "", bool isVirtualManager = false)
        {
            IsVirtualManager = isVirtualManager;
            Manager = manager;
            Name = name;
            Url = url;
            if (manager.Capabilities.Sources.KnowsPackageCount)
                PackageCount = packageCount;

            UpdateDate = updateDate;
        }

        public override string ToString()
        {
            if (Manager.Capabilities.SupportsCustomSources)
                return Manager.Name + ": " + Name;
            else
                return Manager.Name;
        }

        /// <summary>
        /// Replaces the current URL with the new one. Must be used only when a placeholder URL is used.
        /// </summary>
        /// <param name="newUrl"></param>
        public void ReplaceUrl(Uri newUrl)
        {
            Url = newUrl;
        }
    }
}
