using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ELClass.services
{
    public class ZoomAuthService
    {
        private readonly IConfiguration _configuration;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public ZoomAuthService(IConfiguration config)
        {
            _configuration = config;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            string zoomTokenUrl = _configuration["ZoomSettings:AuthUrl"];
            string clientId = _configuration["ZoomSettings:ClientId"];
            string clientSecret = _configuration["ZoomSettings:ClientSecret"];
            string encodedCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var requestData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "account_credentials"),
                new KeyValuePair<string, string>("account_id",  _configuration["ZoomSettings:AccountId"])
            };

                var requestContent = new FormUrlEncodedContent(requestData);
                var response = await client.PostAsync(zoomTokenUrl, requestContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var tokenResponse = JsonConvert.DeserializeObject<ZoomTokenResponse>(responseString);
                    return tokenResponse.access_token;
                }
                return null;
            }
        }
    }
    public class ZoomTokenResponse
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }
}
