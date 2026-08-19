using System.Text.RegularExpressions;

namespace TaindSoft.Core
{
    public static partial class RegexDefine
    {

        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        public static partial Regex EmailRegex();

        [GeneratedRegex(@"^[a-z0-9\-]+$")]
        public static partial Regex SlugRegex();

        [GeneratedRegex("(?i)(password)\\s*=\\s*[^;]+", RegexOptions.Compiled, "en-US")]
        public static partial Regex SecretPwdRegex();
    }
}
