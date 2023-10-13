using System.Text.RegularExpressions;

namespace SMM {
    public static class FilterPatternHelpers {

        private static bool HasFilterCharacters(string input) { return Regex.IsMatch(input, @"(^([~\*]+)|(\*)$)"); }
        public static string AddFilterOptionsIfNotSpecified(this string Pattern, FilterOptions options = FilterOptions.None) {
            string result = Pattern.Trim();

            if (HasFilterCharacters(Pattern)) return result;

            if (options.HasFlag(FilterOptions.Contains)) {
                result = $"*{result}*";
            } else {
                if (options.HasFlag(FilterOptions.StartsWith)) {
                    result = $"{result}*";
                }
                if (options.HasFlag(FilterOptions.EndsWith)) {
                    result = $"*{result}";
                }
            }
            if (options.HasFlag(FilterOptions.IgnoreCase)) {
                result = $"~{result}";
            }

            return result;
        }

        public enum FilterOptions {
            None = 0,
            IgnoreCase = 1,
            Contains = 2,
            StartsWith = 4,
            EndsWith = 8,
        }
    }
}
