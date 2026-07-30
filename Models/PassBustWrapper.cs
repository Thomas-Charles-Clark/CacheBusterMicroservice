
namespace CacheBusterMicroservice.Models
{
	public class PassBustWrapper
	{
		public byte target { get; set; }
		public IEnumerable<PassBust> items { get; set; } = [];
	}
}
