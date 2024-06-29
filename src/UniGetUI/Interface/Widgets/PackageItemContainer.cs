using Microsoft.UI.Xaml.Controls;
using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.Interface.Widgets
{
    public class PackageItemContainer : ItemContainer
    {
#pragma warning disable CS8618
        public IPackage? Package { get; set; }
        public PackageWrapper Wrapper { get; set; }
    }
#pragma warning restore CS8618
}
