namespace Mega.Client
{
	/// <summary>
	/// Directs feedback to a black hole. Used when the caller does not provide any feedback sink.
	/// </summary>
	internal sealed class DummyFeedbackChannel : IFeedbackChannel
	{
		public static readonly DummyFeedbackChannel Default = new DummyFeedbackChannel();

		public string Status { get; set; }
		public double? Progress { get; set; }

		public IFeedbackChannel BeginSubOperation(string name)
		{
			return Default;
		}

		public void Dispose()
		{
		}

		public void WriteWarning(string message)
		{
		}

		public void WriteVerbose(string message)
		{
		}

		public void WriteWarning(string formatString, params object[] arguments)
		{
		}

		public void WriteVerbose(string formatString, params object[] arguments)
		{
		}
	}
}