namespace ModelFiltersGenerator
{
    internal static class StringExtensions
    {
        internal static string ToCamelCase(this string str)
        {
            if (str.Length == 0) return str;

            var firstLetter = str[0].ToString().ToLower();
            return firstLetter + str.Substring(1);
        }

        internal static string Pluralize(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (str.EndsWith("s")
                || str.EndsWith("x")
                || str.EndsWith("ch")
                || str.EndsWith("sh"))
            {
                return str + "es";
            }

            if (str.EndsWith("y"))
            {
                return str.Substring(0, str.Length - 1) + "ies";
            }

            return str + "s";
        }
    }
}
