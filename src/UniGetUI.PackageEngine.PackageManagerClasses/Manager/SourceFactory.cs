using UniGetUI.PackageEngine.Interfaces;
using UniGetUI.PackageEngine.ManagerClasses.Manager;

namespace UniGetUI.PackageEngine.Classes.Manager
{
    public class SourceFactory: ISourceFactory
    {
        private readonly IPackageManager __manager;
        private readonly Dictionary<string, IManagerSource> __reference;
        private readonly Uri __default_uri = new("https://marticliment.com/unigetui/");

        public SourceFactory(PackageManager manager)
        {
            __reference = new();
            __manager = manager;
        }

        public void Reset()
        {
            __reference.Clear();
        }

        /// <summary>
        /// Returns the existing source for the given name, or creates a new one if it does not exist.
        /// </summary>
        /// <param name="name">The name of the source</param>
        /// <returns>A valid ManagerSource</returns>
        public IManagerSource GetSourceOrDefault(string name)
        {
            IManagerSource? source;
            if (__reference.TryGetValue(name, out source) && source != null)
            {
                return source;
            }

            ManagerSource new_source = new(__manager, name, __default_uri);
            __reference.Add(name, new_source);
            return new_source;
        }

        /// <summary>
        /// Returns the existing source for the given name, or null if it does not exist.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IManagerSource? GetSourceIfExists(string name)
        {
            IManagerSource? source;
            if (__reference.TryGetValue(name, out source))
            {
                return source;
            }
            return null;
        }

        public void AddSource(IManagerSource source)
        {
            if (!__reference.TryAdd(source.Name, source))
            {
                IManagerSource existing_source = __reference[source.Name];
                if (existing_source.Url == __default_uri)
                    existing_source.ReplaceUrl(source.Url);
            }
        }

        public IManagerSource[] GetAvailableSources()
        {
            return __reference.Values.ToArray();
        }
    }
}
