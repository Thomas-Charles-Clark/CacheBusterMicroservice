using CacheBusterMicroservice.Models;
using CacheBusterMicroservice.Utilities;

using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using System.Net.Mime;

using System.Text;

namespace CacheBusterMicroservice.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ProductPagesController(DatabaseContext dbContext) : ControllerBase
	{
		[HttpGet("urls")]
		// Informs the client that the incoming network response stream contains raw JSON data
		[Produces("application/json")]
		public async Task URLs(CancellationToken cancellationToken)
		{
			Response.ContentType = "application/json";

			var parameters = new { };

			// Stream directly from the SQL Network buffer into the HTTP response stream
			await foreach (var chunk in dbContext.StreamProductPageURLs(parameters, cancellationToken))
			{
				await Response.WriteAsync(chunk, cancellationToken);
			}
		}
	}
}
