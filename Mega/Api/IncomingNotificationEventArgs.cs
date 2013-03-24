namespace Mega.Api
{
	using System;
	using Newtonsoft.Json.Linq;
	using Useful;

	public sealed class IncomingNotificationEventArgs : EventArgs
	{
		public JObject Command { get; private set; }

		public bool Handled { get; set; }

		public IncomingNotificationEventArgs(JObject command)
		{
			Argument.ValidateIsNotNull(command, "command");

			Command = command;
		}
	}
}