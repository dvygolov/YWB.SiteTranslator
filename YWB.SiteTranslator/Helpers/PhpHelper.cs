using System.Text.RegularExpressions;

namespace YWB.SiteTranslator.Helpers
{
    public class PhpHelper
    {
        public static string RemovePhp(string html)
        {
            return Regex.Replace(html, @"<\?.*?(\?>|$)", string.Empty,RegexOptions.Singleline);
        }

        public static string RestorePhpTags(string html)
        {
            html = Regex.Replace(html, @"<!--\?", "<?");
            html = Regex.Replace(html, @"\?-->", "?>");
            return html;
        }

        public static string CorrectPhpVariables(string html)
        {
            return Regex.Replace(html, @"&lt;\?\=(\$[^?]+)\?&gt;", @"<?=$1?>");
        }

    }
}
