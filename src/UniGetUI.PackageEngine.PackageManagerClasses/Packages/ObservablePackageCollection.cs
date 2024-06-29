using System.ComponentModel;
using UniGetUI.Core.Classes;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.PackageClasses
{
    /// <summary>
    /// A special ObservableCollection designed to work with Package objects
    /// </summary>
    public class ObservablePackageCollection : SortableObservableCollection<PackageWrapper>
    {
        public enum Sorter
        {
            Checked,
            Name,
            Id,
            Version,
            NewVersion,
            Source,
        }

        public ObservablePackageCollection()
            : base()
        {
            SortingSelector = x => x.Package.Name;
        }


        /// <summary>
        /// Add a package to the collection
        /// </summary>
        /// <param name="p"></param>
        public void Add(IPackage p)
        {
            base.Add(new PackageWrapper(p));
        }

        /// <summary>
        /// Sets the property with which to filter the package and sorts the collection
        /// </summary>
        /// <param name="field">The field with which to sort the collection</param>
        public void SetSorter(Sorter field)
        {
            switch (field)
            {
                case Sorter.Checked:
                    SortingSelector = x => x.Package.IsChecked;
                    break;

                case Sorter.Name:
                    SortingSelector = x => x.Package.Name;
                    break;

                case Sorter.Id:
                    SortingSelector = x => x.Package.Id;
                    break;

                case Sorter.Version:
                    SortingSelector = x => x.Package.VersionAsFloat;
                    break;

                case Sorter.NewVersion:
                    SortingSelector = x => x.Package.NewVersionAsFloat;
                    break;

                case Sorter.Source:
                    SortingSelector = x => x.Package.SourceAsString;
                    break;
            }
        }

        /// <summary>
        /// Clears the collection, deleting the wrapper objects in the process
        /// </summary>
        public new void Clear()
        {
            foreach(PackageWrapper wrapper in this)
            {
                wrapper.Dispose();
            }
            base.Clear();
            GC.Collect();
        }

        /// <summary>
        /// Returns a list containing the packwges in this collection
        /// </summary>
        /// <returns></returns>
        public List<IPackage> GetPackages()
        {
            List<IPackage> packages = new();
            foreach (PackageWrapper wrapper in this)
                    packages.Add(wrapper.Package);
            return packages;
        }

        /// <summary>
        /// Returns a list containing the checked packages on this collection
        /// </summary>
        /// <returns></returns>
        public List<IPackage> GetCheckedPackages()
        { 
            List<IPackage> packages = new();
            foreach (PackageWrapper wrapper in this)
            {
                if (wrapper.Package.IsChecked)
                    packages.Add(wrapper.Package);
            }
            return packages;
        }
        
        /// <summary>
        /// Mark all packages as checked
        /// </summary>
        public void SelectAll()
        {
            foreach (PackageWrapper wrapper in this)
                wrapper.Package.IsChecked = true;
        }
        
        /// <summary>
        /// Mark all packages as unchecked
        /// </summary>
        public void ClearSelection()
        {
            foreach (PackageWrapper wrapper in this)
                wrapper.Package.IsChecked = false;
        }
    }
}
