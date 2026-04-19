namespace Models
{
    public class Testimonial
    {
        public int Id { get; set; }

        /// <summary>"en" or "ar"</summary>
        public string Language { get; set; } = "en";

        /// <summary>null for English; "SA","AE","KW","BH","OM","EG","QA" for Arabic</summary>
        public string? CountryCode { get; set; }

        public string? Name { get; set; }

        public string? Quote { get; set; }

        /// <summary>Rating from 1.0 to 5.0</summary>
        public decimal Rating { get; set; } = 5.0m;

        /// <summary>Stored filename only (in wwwroot/uploads/testimonials/)</summary>
        public string? ImageFileName { get; set; }

        public int SortOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
