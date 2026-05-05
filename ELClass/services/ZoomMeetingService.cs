using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ELClass.services
{
    public class ZoomMeetingService
    {
        private readonly ZoomAuthService _zoomAuthService;
        private readonly IConfiguration _config;

        public ZoomMeetingService(ZoomAuthService zoomAuthService, IConfiguration config)
        {
            _zoomAuthService = zoomAuthService;
            _config = config;
        }

        public async Task<ZoomMeetingResponse> CreateMeetingAsync(string topic, int duration)
        {
            string accessToken = await _zoomAuthService.GetAccessTokenAsync();
            string zoomApiUrl = $"{_config["ZoomSettings:APIBaseUrl"]}users/me/meetings";
            var body = new
            {
                topic = topic,
                type = 2, // Scheduled Meeting
                duration = duration,
                timezone = "UTC",
                settings = new { host_video = true, participant_video = true }
            };
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var jsonContent = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(zoomApiUrl, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<ZoomMeetingResponse>(responseString);
                }
            }
            return null;
        }
    }
    public class ZoomMeetingResponse
    {
        [JsonProperty("id")]
        public long MeetingId { get; set; }
        [JsonProperty("join_url")]
        public string? JoinUrl { get; set; }
        [JsonProperty("start_url")]
        public string? StartUrl { get; set; }
    }
}
