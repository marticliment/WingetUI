using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniGetUI.PackageEngine.Interfaces
{
    public interface IManagerSource
    {
        public string IconId { get; }
        public bool IsVirtualManager { get; }
        public IPackageManager Manager { get; }
        public string Name { get; }
        public Uri Url { get; protected set; }
        public int? PackageCount { get; }
        public string? UpdateDate { get; }

        /// <summary>
        /// Returns a human-readable string representing the source name
        /// </summary>
        /// <returns></returns>
        public string ToString();

        /// <summary>
        /// Replaces the current URL with the new one. Must be used only when a placeholder URL is used.
        /// </summary>
        /// <param name="newUrl"></param>
         void ReplaceUrl(Uri newUrl)
         {
             Url = newUrl;
         }
    }
}
