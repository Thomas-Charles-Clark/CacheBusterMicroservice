using CacheBusterMicroservice.Models;
using CacheBusterMicroservice.Utilities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Net;
using System.Net.Mime;
using System.Text;

namespace CacheBusterMicroservice.Controllers
{
	[ApiController]
    [Route("[controller]")]
    public class CacheBustingController : ControllerBase
	{
		private static readonly HttpClient Client = new();

		public CacheBustingController()
        {

        }

		[HttpPost()]
		[Consumes(MediaTypeNames.Application.Json)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> Post(PassBust objBust)
		{
			var targetUrl = $"{Framework.HttpsPrefix}{Framework.SiteHostNames[objBust.target]}{Framework.CacheBustingApiSingularSuffix}";
			var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.SiteUsername}:{Credentials.SitePassword}"));

			// Use HttpClient for modern HTTP requests
			Parallel.ForEach(objBust.items, new ParallelOptions() { }, async (item) =>
			{
				try
				{
					using var requestMessage = new HttpRequestMessage(HttpMethod.Post, targetUrl);

					requestMessage.Headers.Add("cacheBustingKey", Credentials.CacheBustingKey);

					requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthToken);

					requestMessage.Headers.UserAgent.ParseAdd("DesignersGuild_Cache_Bust");

					requestMessage.Headers.Host = Framework.SiteHostNames[objBust.target];

					// Serialize the item to JSON and set it as the request content
					requestMessage.Content = JsonContent.Create(item);

					// Send the request and get the response
					using var response = await Client.SendAsync(requestMessage);

					if (response.IsSuccessStatusCode)
					{
						var responseContent = await response.Content.ReadAsStringAsync();
						var obj = JsonConvert.DeserializeObject(responseContent);
						var str = JsonConvert.SerializeObject(obj, Formatting.Indented);
						Console.WriteLine(str);
					}
				}
				catch (HttpRequestException ex)
				{
					// Handle HTTP errors safely (e.g., 404, 500, timeouts)
					// Log or process the exception details
#if DEBUG
					Console.WriteLine($"Request error: {ex.Message}");
#endif
				}
			});

			return Ok(new { });
		}
    }
}
