namespace Mega.Client
{
	using System;
	using System.Diagnostics;
	using Useful;

	/// <summary>
	/// Directs all feedback to the Debug output, visible to an attached debugger or test harness.
	/// </summary>
	public sealed class DebugFeedbackChannel : IFeedbackChannel
	{
		public string OperationName { get; private set; }

		public void Dispose()
		{
			Debug.WriteLine(string.Format("{0:T} Operation complete.", DateTime.Now), OperationName);
		}

		public string Status
		{
			get { return _status; }
			set
			{
				_status = value;
				Debug.WriteLine(string.Format("{1:T} {0}", Status, DateTime.Now), OperationName);
			}
		}

		public double? Progress
		{
			get { return _progress; }
			set
			{
				_progress = value;

				if (value.HasValue)
				{
					if (Math.Abs(_progress.Value - _lastReportedProgress) >= 0.1 || _progress.Value == 1)
					{
						_lastReportedProgress = _progress.Value;

						Debug.WriteLine(string.Format("{1:T} {0:P0} complete.", _progress.Value, DateTime.Now), OperationName);
					}
				}
			}
		}

		public DebugFeedbackChannel(string operationName)
		{
			Argument.ValidateIsNotNull(operationName, "operationName");

			OperationName = operationName;
		}

		public IFeedbackChannel BeginSubOperation(string name)
		{
			return new DebugFeedbackChannel(OperationName + " / " + name);
		}

		public void WriteWarning(string message)
		{
			WriteWarning("{0}", message);
		}

		public void WriteVerbose(string message)
		{
			WriteVerbose("{0}", message);
		}

		public void WriteWarning(string formatString, params object[] arguments)
		{
			Debug.WriteLine(string.Format("{1:T} Warning: {0}", string.Format(formatString, arguments), DateTime.Now), OperationName);
		}

		public void WriteVerbose(string formatString, params object[] arguments)
		{
			Debug.WriteLine(string.Format("{1:T} Verbose: {0}", string.Format(formatString, arguments), DateTime.Now), OperationName);
		}

		private string _status;
		private double? _progress;
		private double _lastReportedProgress;
	}
}