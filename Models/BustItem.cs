using Newtonsoft.Json;

namespace CacheBusterMicroservice.Models
{
	public class BustItem
	{
		public byte? templateId { get; set; }
		public DateTimeOffset? explicitExpiry { get; set; }
		public DateTime contentDate { get; set; }
		public bool? tradeSwitch { get; set; }
		public string contentType { get; set; }
		public long contentId { get; set; }
		public short countryId { get; set; }
		public byte languageId { get; set; }
		public IEnumerable<long> pageSectionIds { get; set; }

		public BustItem(byte? templateId,
			DateTimeOffset? explicitExpiry,
			DateTime contentDate,
			bool? tradeSwitch,
			string contentType,
			long contentId,
			short countryId,
			byte languageId,
			IEnumerable<long>? pageSectionIds = null)
		{
			this.templateId = templateId;
			this.explicitExpiry = explicitExpiry;
			this.contentDate = contentDate;
			this.tradeSwitch = tradeSwitch;
			this.contentType = contentType;
			this.contentId = contentId;
			this.countryId = countryId;
			this.languageId = languageId;
			this.pageSectionIds = pageSectionIds ?? [];
		}

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}
}
