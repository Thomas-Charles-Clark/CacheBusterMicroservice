using CacheBusterMicroservice.Models;
using CacheBusterMicroservice.Utilities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Collections.Concurrent;
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

		// Adjusted for a powerful 8-vCPU VM hitting itself through Cloudflare - CF have global rate limiting rules and 
		// there isn't much need to go faster than this anyway
		private const int MaxConcurrentRequests = 100;

		/// <summary>
		/// Post
		/// </summary>
		/// <param name="objBust">Contains byte Target and IEnumerable<BustItem></param>
		/// <returns></returns>
		[HttpPost]
		[Consumes(MediaTypeNames.Application.Json)]
		[ProducesResponseType(typeof(BustSummaryResult), StatusCodes.Status200OK)]
		public async Task<IActionResult> Post(PassBust objBust)
		{
			if (objBust?.items == null || !objBust.items.Any())
			{
				return Ok(new BustSummaryResult());
			}

			var targetUrl = $"{Framework.HttpsPrefix}{Framework.SiteHostNames[objBust.target]}{Framework.CacheBustingApiSingularSuffix}";
			var basicAuthToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Credentials.SiteUsername}:{Credentials.SitePassword}"));

			var result = await ProcessCacheBustItemsAsync(objBust.items, targetUrl, basicAuthToken);

			return Ok(result);
		}

		private static async Task<BustSummaryResult> ProcessCacheBustItemsAsync(
			IEnumerable<object> items,
			string targetUrl,
			string basicAuthToken)
		{
			var errors = new ConcurrentBag<ItemError>();
			int successfulCount = 0;
			int failedCount = 0;

			using var semaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
			using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));

			var tasks = items.Select(async (item) =>
			{
				await semaphore.WaitAsync(cts.Token);

				try
				{
					using var requestMessage = new HttpRequestMessage(HttpMethod.Post, targetUrl);

					requestMessage.Headers.Add("cacheBustingKey", Credentials.CacheBustingKey);
					requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthToken);
					requestMessage.Headers.UserAgent.ParseAdd("DesignersGuild_Cache_Bust");

					requestMessage.Content = JsonContent.Create(item);

					using var response = await Client.SendAsync(requestMessage, cts.Token);

					if (response.IsSuccessStatusCode)
					{
						Interlocked.Increment(ref successfulCount);
#if DEBUG
						var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
						var obj = JsonConvert.DeserializeObject(responseContent);
						var str = JsonConvert.SerializeObject(obj, Formatting.Indented);
						Console.WriteLine(str);
#endif
					}
					else
					{
						Interlocked.Increment(ref failedCount);

#if DEBUG
						var responseContent = await response.Content.ReadAsStringAsync(cts.Token);
						var obj = JsonConvert.DeserializeObject(responseContent);
						var str = JsonConvert.SerializeObject(obj, Formatting.Indented);
						Console.WriteLine(str);
#endif

						var thisObj = JObject.FromObject(item).ToObject<BustItem>();

						errors.Add(new ItemError
						{
							ItemSummary = thisObj!.ToString(),
							ErrorMessage = $"Server responded with status code: {response.StatusCode}"
						});
					}
				}
				catch (OperationCanceledException)
				{
					Interlocked.Increment(ref failedCount);

					var thisObj = JObject.FromObject(item).ToObject<BustItem>();

					errors.Add(new ItemError
					{
						ItemSummary = thisObj!.ToString(),
						ErrorMessage = "Request timed out (5-minute batch limit reached)."
					});
				}
				catch (Exception ex)
				{
					Interlocked.Increment(ref failedCount);

					var thisObj = JObject.FromObject(item).ToObject<BustItem>();

					errors.Add(new ItemError
					{
						ItemSummary = thisObj!.ToString(),
						ErrorMessage = ex.Message
					});
				}
				finally
				{
					semaphore.Release();
				}
			});

			await Task.WhenAll(tasks);

			return new BustSummaryResult
			{
				TotalProcessed = successfulCount + failedCount,
				SuccessfulCount = successfulCount,
				FailedCount = failedCount,
				Errors = errors.ToList()
			};
		}
	}
}
