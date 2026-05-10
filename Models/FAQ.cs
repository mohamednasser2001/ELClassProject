namespace Models
{
    public class FAQ
    {
        public int Id { get; set; }

        /// <summary>"en" or "ar"</summary>
        public string Language { get; set; } = "en";

        /// <summary>null for English; "SA","AE","KW","BH","OM","EG","QA" for Arabic</summary>
        public string? CountryCode { get; set; }

        public string? Question { get; set; }

        public string? Answer { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
