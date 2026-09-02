namespace CacheBusterMicroservice.Utilities
{
	public static class Framework
	{
		public static readonly string CacheBustingApiSingularSuffix = "/api/cacheapi/purge";
		public static readonly string CacheBustingApiMultipleSuffix = "/api/cacheapi/purgeMultiple";
		public static readonly string HttpsPrefix = "https://";

		public static readonly string[] SiteHostNames =
		[
			"localhost:44338",
			"flock-dev.designersguild.com",
			"flock-preview.designersguild.com",
			"www.designersguild.com"
		];
	}
}
