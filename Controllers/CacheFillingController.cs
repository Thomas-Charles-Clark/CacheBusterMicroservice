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

namespace CacheBusterMicroservice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CacheFillingController : ControllerBase
	{
		private static readonly HttpClient Client = new();

		// Generate a fake but perfectly structured 24-character alphanumeric ASP.NET Session ID
		private const string StaticSessionID = "b4f2c5d1e6a7f8e9c0b1a2d3";

		// Adjusted for a powerful 8-vCPU VM hitting itself through Cloudflare - CF have global rate limiting rules and 
		// there isn't much need to go faster than this anyway
		private const int MaxConcurrentRequests = 1000;

		public CacheFillingController()
        {

        }
		
		/// <summary>
		/// Fill
		/// </summary>
		/// <param name="objBust">Contains byte Target and IEnumerable<BustItem></param>
		/// <returns></returns>
		[HttpPost]
		[Consumes(MediaTypeNames.Application.Json)]
		[ProducesResponseType(typeof(FillSummaryResult), StatusCodes.Status200OK)]
		public async Task<IActionResult> Fill(PassFill objBust)
		{
			if (objBust?.URLs == null || !objBust.URLs.Any())
			{
				return Ok(new FillSummaryResult());
			}

			var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.SiteUsername}:{Credentials.SitePassword}"));

			var result = await ProcessCacheFillItemsAsync(objBust.URLs, basicAuthToken);

			Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));

			return Ok(result);
		}

		public static async Task<FillSummaryResult> ProcessCacheFillItemsAsync(IEnumerable<string> URLs, string basicAuthToken)
		{
			var errors = new ConcurrentBag<ItemError>();
			int successfulCount = 0;
			int failedCount = 0;
			using var semaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
			using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

			var tasks = URLs.Select(async (url) =>
			{
				await semaphore.WaitAsync(cts.Token);

				try
				{
					var uri = new Uri(url);
					string hostHeader = uri.Host;

					using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);

					requestMessage.Headers.Host = hostHeader;
					requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthToken);
					requestMessage.Headers.UserAgent.ParseAdd("DesignersGuild_Cache_Fill");

					// Manually inject the ASP.NET Session ID cookie header
					requestMessage.Headers.Add("Cookie", $"ASP.NET_SessionId={StaticSessionID}");

					using var response = await Client.SendAsync(requestMessage, cts.Token);

					if (response.IsSuccessStatusCode)
					{
						Interlocked.Increment(ref successfulCount);

#if DEBUG
						//var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
						//var obj = JsonConvert.DeserializeObject(responseContent);
						//var str = JsonConvert.SerializeObject(obj, Formatting.Indented);
						//Console.WriteLine(str);
#endif

					}
					else
					{
						var failureResponse = await response.Content.ReadAsStringAsync();

						Console.WriteLine(failureResponse);

						Interlocked.Increment(ref failedCount);

						errors.Add(new ItemError
						{
							ItemSummary = url,
							ErrorMessage = $"Server responded with status code: {response.StatusCode}"
						});
					}
				}
				catch (OperationCanceledException oce)
				{
					Console.WriteLine(oce);

					Interlocked.Increment(ref failedCount);

					errors.Add(new ItemError
					{
						ItemSummary = url,
						ErrorMessage = "Request timed out (5-minute batch limit reached)."
					});
				}
				catch (Exception ex)
				{
					Interlocked.Increment(ref failedCount);

					errors.Add(new ItemError
					{
						ItemSummary = url,
						ErrorMessage = ex.Message
					});
				}
				finally
				{
					semaphore.Release();
				}
			});

			await Task.WhenAll(tasks);

			return new FillSummaryResult
			{
				TotalProcessed = successfulCount + failedCount,
				SuccessfulCount = successfulCount,
				FailedCount = failedCount,
				Errors = errors.ToList()
			};
		}
    }
}
