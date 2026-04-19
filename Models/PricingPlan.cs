namespace Models
{
    /// <summary>
    /// A single pricing tier on the homepage.
    /// Features are stored as newline-separated items, each formatted as "Feature text|1" (1=checked) or "Feature text|0" (unchecked).
    /// Example: "Unlimited courses|1\nFree eBook downloads|0"
    /// </summary>
    public class PricingPlan
    {
        public int Id { get; set; }

        /// <summary>"en" or "ar"</summary>
        public string Language { get; set; } = "en";

        /// <summary>null for English; "SA","AE","KW","BH","OM","EG","QA" for Arabic</summary>
        public string? CountryCode { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        /// <summary>Currency symbol, e.g. "$" or "ر.س"</summary>
        public string? Currency { get; set; } = "$";

        public string? MonthlyPrice { get; set; }

        public string? YearlyPrice { get; set; }

        /// <summary>Marks this plan as "Most Popular"</summary>
        public bool IsPopular { get; set; } = false;

        /// <summary>
        /// Newline-separated feature rows. Each row: "Feature text|1" or "Feature text|0".
        /// </summary>
        public string? Features { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // ── Helper (not mapped) ──────────────────────────────────────────────────
        public List<(string Text, bool Checked)> ParsedFeatures()
        {
            var result = new List<(string, bool)>();
            if (string.IsNullOrWhiteSpace(Features)) return result;
            foreach (var line in Features.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split('|');
                var text = parts[0].Trim();
                var isChecked = parts.Length > 1 && parts[1].Trim() == "1";
                result.Add((text, isChecked));
            }
            return result;
        }
    }
}
