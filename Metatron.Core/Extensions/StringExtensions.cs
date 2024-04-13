using System;

namespace Metatron.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comparison)
        {
            if (source == null && toCheck == null)
                return true;
            
            if (source == null || toCheck == null)
                return false;

            return source.IndexOf(toCheck, comparison) >= 0;
        }
    }
}
