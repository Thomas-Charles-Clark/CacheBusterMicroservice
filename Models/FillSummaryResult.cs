namespace CacheBusterMicroservice.Models
{
	public class FillSummaryResult
	{
		public int TotalProcessed { get; set; }
		public int SuccessfulCount { get; set; }
		public int FailedCount { get; set; }
		public List<ItemError> Errors { get; set; } = [];
	}
}
