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
    }
}
