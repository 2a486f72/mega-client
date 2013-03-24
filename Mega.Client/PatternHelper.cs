namespace Mega.Client
{
	using System.Diagnostics;
	using System.Threading;

	/// <summary>
	/// Provides methods that help make use of the common patterns used in the Mega client library.
	/// </summary>
	internal static class PatternHelper
	{
		public static void LogMethodCall(string methodName, IFeedbackChannel feedbackChannel, CancellationToken cancellationToken)
		{
			Debug.WriteLine("{0} called. Has feedback channel: {1}  Is cancelable: {2}", methodName, feedbackChannel != null, cancellationToken.CanBeCanceled);
		}

		/// <summary>
		/// Ensures that we have a feedback channel, creating a dummy one if needed.
		/// Avoids bothersome conditional logic related to feedback - we always provide it.
		/// </summary>
		public static void EnsureFeedbackChannel(ref IFeedbackChannel feedbackChannel)
		{
			if (feedbackChannel != null)
				return;

			feedbackChannel = DummyFeedbackChannel.Default;
		}
	}
}