namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	public sealed class NewItemsResult
	{
		[JsonProperty("f")]
		public Item[] Items { get; set; }
	}
}