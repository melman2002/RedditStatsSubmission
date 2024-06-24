using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;

namespace RedditStats.Server.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class StatsController : ControllerBase
	{
		private readonly ILogger<StatsController> _logger;
        private static readonly long _appStartTime = DateTimeOffset.Now.ToUnixTimeSeconds();
		private static readonly HttpClient _httpClient = new();
        private static TokenInfo _token;
		private static long _tokenExpiration = 0;
		private static string _afterAnchor = "";

        private static readonly string _redditAuthUrl = "https://www.reddit.com/api/v1/access_token";
        private static readonly string _clientId = "[Insert your clientId here]";
        private static readonly string _clientSecret = "[Insert your clientSecret here]";
        private static readonly string _subRedditName = "worldnews";

        public StatsController(ILogger<StatsController> logger)
		{
			_logger = logger;
		}

		[HttpGet(Name = "GetStats")]
		public async Task<Stats> Get(long startTime = 0)
		{
			if (startTime == 0)
				startTime = _appStartTime;

			try
            {
                return await GetStats(startTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stats");
                return null;
            }
		}

		private async Task<Stats> GetStats(long startTime)
		{
            var posts = new List<Post>();
            var slice = new List<Post>();
            _afterAnchor = ""; //reset the after anchor so we always start with the newest posts

            do
            {
                slice = await GetNextSliceOfStats();
                posts.AddRange(slice);
            } while (!slice.Any(p => p.Created < startTime)); //keep getting older posts until we've passed before the start time

            posts.RemoveAll(p => p.Created < startTime);
            posts = posts.OrderByDescending(p => p.UpVotesCount).ToList();

            var users = (from p in posts
                        group p.Title by p.Username into g
                        select new User { Name = g.Key, PostsCount = g.Count() }).ToList();
            users = users.OrderByDescending(u => u.PostsCount).ToList();

            return new Stats()
            {
                Posts = posts,
                Users = users
            };
		}

        private async Task<List<Post>> GetNextSliceOfStats()
        {
            if (_token == null || DateTimeOffset.Now.ToUnixTimeSeconds() > _tokenExpiration)
            {
                _token = await GetAccessToken();
                _tokenExpiration = DateTimeOffset.Now.ToUnixTimeSeconds() + _token.expires_in - 5; //give ourselves a 5 second buffer to renew token
				_httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_token.token_type, _token.access_token);
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(("application/json")));
            }

            var response = await _httpClient.GetAsync($"https://oauth.reddit.com/r/{_subRedditName}/new?after={_afterAnchor}&show=all");

            PaceRequests(response);

            dynamic responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            var slice = new List<Post>();
            foreach (var child in responseObject?.data.children)
            {
                slice.Add(new Post
                {
                    Created = child.data.created,
                    Title = child.data.title,
                    UpVotesCount = child.data.ups,
                    Username = child.data.author,
                    Url = child.data.permalink
                });
            }
            _afterAnchor = responseObject?.data.after ?? ""; //This will be used as a marker for the next slice

            return slice;
        }

        private void PaceRequests(HttpResponseMessage response)
        {
            //decide if we need to sleep in order to pace our requests
            var rateLimitRemaining = GetHeaderAsDecimal("x-ratelimit-remaining", response);
            var rateLimitUsed = GetHeaderAsDecimal("x-ratelimit-used", response);
            var rateLimitReset = GetHeaderAsDecimal("x-ratelimit-reset", response) + 5; //seconds left to reset limit + 5 second buffer

            var percentUsed = rateLimitUsed / (rateLimitRemaining + rateLimitUsed);
            var percentTimeUsed = 1 - rateLimitReset / 600; //10 minute limit

            if(percentUsed > percentTimeUsed)
            {
                //sleep to wait for percentTimeUsed to catch up (includes a 5 second buffer)
                var percentToWait = percentUsed - percentTimeUsed;
                var millisecondsToWait = Convert.ToInt32(Math.Floor(percentToWait * 600 * 1000));
                Thread.Sleep(millisecondsToWait);
            }
        }

        private decimal GetHeaderAsDecimal(string header, HttpResponseMessage response) 
        {
            return Convert.ToDecimal(response.Headers.Where(h => h.Key == header).First().Value.First());
        }

		private async Task<TokenInfo> GetAccessToken()
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; AcmeInc/1.0)");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}")));
            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpClient.PostAsync(_redditAuthUrl, content);

			if(response.IsSuccessStatusCode)
            {
                return System.Text.Json.JsonSerializer.Deserialize<TokenInfo>(await response.Content.ReadAsStringAsync());
            }
            else
            {
                throw (new Exception($"Unable to get access token.\nStatusCode: {response.StatusCode}\nReasonPhrase: {response.ReasonPhrase}"));
            }
        }

        private class TokenInfo
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string scope { get; set; }
            public string token_type { get; set; }
        }
    }
}
