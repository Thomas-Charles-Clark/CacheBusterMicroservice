using CacheBusterMicroservice.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mime;
using CacheBusterMicroservice.Utilities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace CacheBusterMicroservice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CacheInspectorController : ControllerBase
	{
		private static readonly HttpClient Client = new();

		// Generate a fake but perfectly structured 24-character alphanumeric ASP.NET Session ID
		private const string StaticSessionID = "b4f2c5d1e6a7f8e9c0b1a2fa";

		// Adjusted for a powerful 8-vCPU VM hitting itself through Cloudflare - CF have global rate limiting rules and 
		// there isn't much need to go faster than this anyway
		private const int MaxConcurrentRequests = 1000;

		public CacheInspectorController()
        {

        }
		
		/// <summary>
		/// List
		/// </summary>
		/// <returns>
		///		Return A list of the names of MemoryCache objects which can be addressed.
		/// </returns>
		[HttpGet("List")]
		[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
		public async Task<IActionResult> List(byte Target)
		{
			var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.SiteUsername}:{Credentials.SitePassword}"));

			string targetUrl = $"{Framework.HttpsPrefix}{Framework.SiteHostNames[Target]}/api/cache/list";

			var result = await GetStreamAsync(targetUrl, basicAuthToken);

			return Ok(result);
		}

		/// <summary>
		///		Inspect
		/// </summary>
		/// <returns>
		///		Stream back contents from named MemoryCache singleton residing in SiteHostNames[target].
		/// </returns> 
		[HttpGet("Inspect")]
		public async Task<IActionResult> Inspect(byte Target, string CacheName)
		{
			try
			{
				string targetUrl = $"{Framework.HttpsPrefix}{Framework.SiteHostNames[Target]}/api/cache/inspect?cacheName={CacheName}";

				var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.SiteUsername}:{Credentials.SitePassword}"));

				var result = await GetStreamAsync(targetUrl, basicAuthToken);

				return Ok(result);
			}
			catch (Exception e)
			{
				return StatusCode((int)HttpStatusCode.InternalServerError, e.Message);
			}
		}

		public static async Task<Stream> GetStreamAsync(string url, string basicAuthToken)
		{
			using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

			// Note: Use HttpMethod.Get if your Web API expects a GET request
			var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

			requestMessage.Headers.Host = new Uri(url).Host;
			requestMessage.Headers.Add("cacheBustingKey", Credentials.CacheBustingKey);
			requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuthToken);
			requestMessage.Headers.UserAgent.ParseAdd("DesignersGuild_Cache_Inspect");
			requestMessage.Headers.Add("Cookie", $"ASP.NET_SessionId={StaticSessionID}");

			// FIX: Send the actual configured request message!
			// ResponseHeadersRead ensures we don't buffer 13k records into RAM at once
			var response = await Client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cts.Token);

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				throw new Exception($"Downstream App error ({response.StatusCode}): {errorContent}");
			}

			// Return the live network stream directly to the controller output
			return await response.Content.ReadAsStreamAsync(cts.Token);
		}
	}
}
