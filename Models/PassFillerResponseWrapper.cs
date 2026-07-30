namespace CacheBusterMicroservice.Models
{
	public class PassFillerResponseWrapper
	{
		public byte target { get; set; }

		public IEnumerable<PassFillerResponse> items { get; set; } = [];
	}
}
