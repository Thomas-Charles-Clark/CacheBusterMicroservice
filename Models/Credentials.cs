namespace CacheBusterMicroservice.Models
{
	public static class Credentials
	{
		public static readonly string SiteUsername = "CAdmin";
		public static readonly string SitePassword = "TapestryFlower22"; // Note that this is for in-app Basic Auth. So that Cloudflare can issue the demand. Our VM-WEB2 Basic Auth password differs.
		public static readonly string CacheBustingKey = "9079C890-8ADB-47C2-8D26-458C773EFEDA";
	}
}
