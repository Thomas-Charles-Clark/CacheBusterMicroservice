namespace CacheBusterMicroservice.Models
{
	public class PassFillerWrapper
	{
		public byte target { get; set; }
		public IEnumerable<PassBust> items { get; set; } = [];
	}
}
