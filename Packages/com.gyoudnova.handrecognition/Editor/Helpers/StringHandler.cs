public static class StringHandler
{
    /// <summary>
    /// A utility method to normalize a string by converting it to Title Case
    /// </summary>
    /// <param name="input">The input string</param>
    /// <returns>Normalized string</returns>
    public static string GetNormalizedString(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Normalize the string to Title Case
        var normalized = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLowerInvariant());

        // Remove any leading or trailing whitespace
        normalized = normalized.Trim();

        return normalized;
    }
}
