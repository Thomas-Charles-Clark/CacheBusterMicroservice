namespace CacheBusterMicroservice.Models
{
	public class PassBust
	{
		/// <summary>
		// 0 = Localhost
		// 1 = Dev{}
		// 2 = Preview
		// 3 = Production
		/// </summary>
		public byte target { get; set; }
		public List<BustItem> items { get; set; } = new List<BustItem>();
	}
}
