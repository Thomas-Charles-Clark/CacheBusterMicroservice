namespace CacheBusterMicroservice.Utilities
{
	public static class Framework
	{
		public static readonly string CacheBustingApiSingularSuffix = "/api/cache/purge";
		public static readonly string CacheBustingApiMultipleSuffix = "/api/cache/purgeMultiple";
		public static readonly string HttpsPrefix = "https://";

		public static readonly string[] SiteHostNames =
		[
			"localhost",
			"flock-dev.designersguild.com",
			"flock-preview.designersguild.com",
			"www.designersguild.com"
		];
	}
}
