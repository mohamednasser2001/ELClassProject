namespace Models
{
    public class Article
    {
        public int Id { get; set; }

        /// <summary>"en" or "ar"</summary>
        public string Language { get; set; } = "en";

        /// <summary>null for English; "SA","AE","KW","BH","OM","EG","QA" for Arabic</summary>
        public string? CountryCode { get; set; }

        public string? Title { get; set; }

        /// <summary>Category tag shown on the card, e.g. "Online Learning", "Technology", "AI"</summary>
        public string? Category { get; set; }

        /// <summary>Stored filename only (in wwwroot/uploads/articles/)</summary>
        public string? ImageFileName { get; set; }

        /// <summary>Short description shown on homepage card</summary>
        public string? Description { get; set; }

        /// <summary>Full article body (HTML allowed)</summary>
        public string? Content { get; set; }

        /// <summary>External or internal URL for the "Read More" button</summary>
        public string? ReadMoreUrl { get; set; }

        public DateTime PublishedDate { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;
    }
}
