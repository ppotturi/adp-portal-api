using System.Text.RegularExpressions;

namespace ADP.Portal.Core.Git.Entities
{
    internal static partial class FluxImagePolicyRegex
    {
        [GeneratedRegex(@"(\d+(?:\.\d+\.\d+)?)(?:-([A-Za-z0-9.-]+))?\s*#\s*\{""\$imagepolicy"":\s*""([^""]+)(:tag)?""\}", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
        public static partial Regex PolicyRegex();
    }
}
