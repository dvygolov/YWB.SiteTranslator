using System.IO;
using System.Reflection;

namespace YWB.SiteTranslator.Helpers
{
    public class PathHelper
    {
        public static string GetFullPath(string element)
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return Path.Combine(dir, element);
        }
    }
}
