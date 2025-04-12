using System.Globalization;

public static class NumberFormatter
{
    // Define thresholds using decimal for precision
    private static readonly decimal GoldThreshold = 1_000_000_000_000_000_000_000_000m; // 10^24
    private static readonly decimal SextillionThreshold = 1_000_000_000_000_000_000_000m; // 10^21
    private static readonly decimal QuintillionThreshold = 1_000_000_000_000_000_000m; // 10^18
    private static readonly decimal QuadrillionThreshold = 1_000_000_000_000_000m;     // 10^15
    private static readonly decimal TrillionThreshold = 1_000_000_000_000m;          // 10^12
    private static readonly decimal BillionThreshold = 1_000_000_000m;             // 10^9
    private static readonly decimal MillionThreshold = 1_000_000m;              // 10^6
    private static readonly decimal ThousandThreshold = 1_000m;                 // 10^3

    /// <summary>
    /// Formats a decimal number into a string with standard abbreviations (K, M, B, T, ...)
    /// for large values or with commas for values below 1000. Uses InvariantCulture.
    /// Adapts precision based on magnitude.
    /// </summary>
    /// <param name="value">The decimal value to format.</param>
    /// <param name="includePlusSign">If true, prepends a '+' sign for positive non-zero values.</param>
    /// <returns>A formatted string representation of the number.</returns>
    public static string FormatNumber(decimal value, bool includePlusSign = false)
    {
        string sign = includePlusSign && value > 0 ? "+" : "";
        string formattedValue;

        if (value == 0) return "0"; // Handle zero explicitly

        // Handle negative numbers by formatting their absolute value and prepending '-'
        if (value < 0)
        {
            return "-" + FormatNumber(-value); // Recursive call for the positive equivalent
        }

        // Note: The order of these checks matters (largest to smallest)
        if (value >= GoldThreshold) 
        {
            decimal result = value / SextillionThreshold;
            formattedValue = FormatWithSuffix(result, "G");
        }
        else if (value >= SextillionThreshold) 
        {
            decimal result = value / SextillionThreshold;
            formattedValue = FormatWithSuffix(result, "Sx");
        }
        else if (value >= QuintillionThreshold) 
        {
            decimal result = value / QuintillionThreshold;
            formattedValue = FormatWithSuffix(result, "Qt");
        }
        else if (value >= QuadrillionThreshold) 
        {
            decimal result = value / QuadrillionThreshold;
            formattedValue = FormatWithSuffix(result, "Qd");
        }
        else if (value >= TrillionThreshold) 
        {
            decimal result = value / TrillionThreshold;
            formattedValue = FormatWithSuffix(result, "T");
        }
        else if (value >= BillionThreshold) 
        {
            decimal result = value / BillionThreshold;
            formattedValue = FormatWithSuffix(result, "B");
        }
        else if (value >= MillionThreshold) 
        {
            decimal result = value / MillionThreshold;
            formattedValue = FormatWithSuffix(result, "M");
        }
        else if (value >= ThousandThreshold) 
        {
            decimal result = value / ThousandThreshold;
            formattedValue = FormatWithSuffix(result, "K");
        }
        else
        {
            // For values less than 1000, decide precision
            // If it's an integer value after checking thresholds
            if (value == decimal.Truncate(value))
            {
                formattedValue = value.ToString("N0", CultureInfo.InvariantCulture); // Format integers with commas
            }
            else
            {
                // Show 1 or 2 decimal places for small non-integers
                formattedValue = value.ToString("F1", CultureInfo.InvariantCulture); // Adjust "F1" or "F2" as needed
            }
        }

        return sign + formattedValue;
    }

    // Helper method to format a value with a suffix, removing decimal places if they're zeros
    private static string FormatWithSuffix(decimal value, string suffix)
    {
        // Check if the value is a whole number or has decimal places
        if (value == decimal.Truncate(value))
        {
            // It's a whole number, so format without decimals
            return $"{decimal.Truncate(value)} {suffix}";
        }
        else
        {
            // Try with 1 decimal place first
            string formatted = value.ToString("F1", CultureInfo.InvariantCulture);
            
            // Check if the last character is '0'
            if (formatted.EndsWith("0"))
            {
                // No decimals needed, use the integer value
                return $"{decimal.Truncate(value)} {suffix}";
            }
            
            // Keep 1 decimal place
            return $"{formatted} {suffix}";
        }
    }
} 