using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class ManagerSource : IManagerSource
    {
        public virtual IconType IconId { get { return Manager.Properties.IconId; } }

        public IPackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; set; }
        public int? PackageCount { get; }
        public string UpdateDate { get; }
        public string AsString { get; protected set; }
        public string AsString_DisplayName { get; protected set; }

        public bool IsVirtualManager { get; }

        public ManagerSource(IPackageManager manager, string name, Uri url, int? packageCount = 0, string updateDate = "", bool isVirtualManager = false)
        {
            IsVirtualManager = isVirtualManager;
            Manager = manager;
            Name = name;
            Url = url;
            if (manager.Capabilities.Sources.KnowsPackageCount)
            {
                PackageCount = packageCount;
            }

            UpdateDate = updateDate;
            AsString = "";
            AsString_DisplayName = "";
            RefreshSourceNames();
        }

        public override string ToString()
        {
            throw new NotImplementedException("Use the `AsString` attribute instead");
        }

        /// <summary>
        /// Replaces the current URL with the new one. Must be used only when a placeholder URL is used.
        /// </summary>
        public void ReplaceUrl(Uri newUrl)
        {
            Url = newUrl;
        }

        /// <summary>
        /// Will refresh the source names based on the current manager's properties.
        /// </summary>
        public void RefreshSourceNames()
        {
            AsString = Manager.Capabilities.SupportsCustomSources ? $"{Manager.Properties.Name}: {Name}" : Manager.Properties.Name;
            if (Manager.Properties.DisplayName is string display_name)
                AsString_DisplayName = Manager.Capabilities.SupportsCustomSources ? $"{display_name}: {Name}" : display_name;
            else
                AsString_DisplayName = AsString;
        }
    }
}
