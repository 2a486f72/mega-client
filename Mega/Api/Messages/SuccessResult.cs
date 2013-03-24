namespace Mega.Api.Messages
{
	using Newtonsoft.Json;

	/// <summary>
	/// Used to report success of an API call that does not have an actual result.
	/// </summary>
	[JsonConverter(typeof(SuccessResultConverter))]
	public struct SuccessResult
	{
	}
}