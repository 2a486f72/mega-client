namespace Mega.Client
{
	using System;

	/// <summary>
	/// Used by the Mega client to provide asynchronous cross-thread feedback about an ongoing operation.
	/// It can also provide feedback channels for sub-operations that the Mega client starts.
	/// </summary>
	/// <remarks>
	/// Mega operations automatically dispose of any sub-operation feedback channels when the sub-operation is complete.
	/// </remarks>
	public interface IFeedbackChannel : IDisposable
	{
		/// <summary>
		/// Gets or sets a general description of the operation status.
		/// </summary>
		string Status { get; set; }

		/// <summary>
		/// Gets or sets an estimate of the progress of the operation (0...1).
		/// </summary>
		double? Progress { get; set; }

		/// <summary>
		/// Informs the feedback object that a new operation or sub-operation has started.
		/// The returned object will be used to provide feedback specific to that operation.
		/// </summary>
		IFeedbackChannel BeginSubOperation(string name);

		void WriteWarning(string message);
		void WriteVerbose(string message);
		void WriteWarning(string format, params object[] arguments);
		void WriteVerbose(string format, params object[] arguments);
	}
}