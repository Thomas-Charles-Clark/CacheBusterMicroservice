using System.Net;

namespace CacheBusterMicroservice.Models
{
	public class PassFillerResponse
	{
		public required string URL { get; set; }
		public HttpStatusCode ResponseCode { get; set; }
	}
}
