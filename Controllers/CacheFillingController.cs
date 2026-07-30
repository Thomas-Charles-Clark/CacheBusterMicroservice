using CacheBusterMicroservice.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Mime;
using CacheBusterMicroservice.Utilities;

namespace CacheBusterMicroservice.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class CacheFillingController : ControllerBase
    {
		private static readonly ParallelOptions parallelOptions = new()
		{
			MaxDegreeOfParallelism = 16
		};

		private static readonly HttpClient client = new HttpClient();

        public CacheFillingController()
        {

        }

        [HttpPost()]
		[Consumes(MediaTypeNames.Application.Json)]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public IActionResult Post(PassFillerWrapper fillerWrapper)
		{
			var objLock = new object();

			var result = new List<PassFillerResponse>();

			client.DefaultRequestHeaders.Clear();

			// splits should end up as an IEnumerable<IEnumerable<PassBust>>
			var chunks = fillerWrapper.items.Chunk(500).ToList();

			foreach (var chunk in chunks)
			{
				Parallel.ForEach(chunk, parallelOptions, (split) => {

					Parallel.ForEach(split.items, parallelOptions, (item) =>
					{
						var url = $"{Framework.HttpsPrefix}{Framework.SiteHostNames[split.target]}{Framework.CacheBustingApiSingularSuffix}";

						var request = new HttpRequestMessage()
						{
							RequestUri = new Uri(url),
							Method = HttpMethod.Get
						};

						request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic",
							Convert.ToBase64String(
								Encoding.ASCII.GetBytes($"{Credentials.SiteUsername}:{Credentials.SitePassword}")
							)
						);

						request.Headers.Add("cacheBustingKey", Credentials.CacheBustingKey);

						request.Headers.Add("User-Agent", "DesignersGuild_Cache_Fill");

						var response = client.SendAsync(request).Result;

						lock (objLock)
						{
							if (response.IsSuccessStatusCode)
							{
								result.Add(new PassFillerResponse()
								{
									URL = url,
									ResponseCode = response.StatusCode
								});
							}
							else
							{
								result.Add(new PassFillerResponse()
								{
									URL = response.Content.ReadAsStringAsync().Result,
									ResponseCode = response.StatusCode
								});
							}
						}
					});
				});
			}

			return Ok();
        }
    }
}
