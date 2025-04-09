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
        if (value >= GoldThreshold) formattedValue = $"{(value / SextillionThreshold):F2} G";
        else if (value >= SextillionThreshold) formattedValue = $"{(value / SextillionThreshold):F2} Sx";
        else if (value >= QuintillionThreshold) formattedValue = $"{(value / QuintillionThreshold):F2} Qt";
        else if (value >= QuadrillionThreshold) formattedValue = $"{(value / QuadrillionThreshold):F2} Qd";
        else if (value >= TrillionThreshold) formattedValue = $"{(value / TrillionThreshold):F2} T";
        else if (value >= BillionThreshold) formattedValue = $"{(value / BillionThreshold):F2} B";
        else if (value >= MillionThreshold) formattedValue = $"{(value / MillionThreshold):F2} M";
        else if (value >= ThousandThreshold) formattedValue = $"{(value / ThousandThreshold):F2} K"; // Added K for consistency
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
} 