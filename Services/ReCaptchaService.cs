using System.Text.Json;

namespace CB_Zad1.Services
{
    public interface IReCaptchaService
    {
        Task<bool> VerifyAsync(string token, string ip);
        string GetSiteKey();
    }

    public class ReCaptchaService : IReCaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;
        private readonly string _siteKey;

        public ReCaptchaService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _secretKey = configuration["ReCaptcha:SecretKey"];
            _siteKey = configuration["ReCaptcha:SiteKey"];
        }

        public string GetSiteKey()
        {
            return _siteKey;
        }

        public async Task<bool> VerifyAsync(string token, string ip)
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_secretKey))
            {
                return false;
            }

            var parameters = new Dictionary<string, string>
            {
                ["secret"] = _secretKey,
                ["response"] = token
            };

            if (!string.IsNullOrWhiteSpace(ip))
            {
                parameters["remoteip"] = ip;
            }

            using var content = new FormUrlEncodedContent(parameters);
            using var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<ReCaptchaResponse>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Success == true && result.Score >= 0.5;
        }

        private sealed class ReCaptchaResponse
        {
            public bool Success { get; set; }
            public double Score { get; set; }
            public string Action { get; set; }
            public string[] ErrorCodes { get; set; }
        }
    }
}