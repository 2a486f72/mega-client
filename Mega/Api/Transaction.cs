namespace Mega.Api
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Threading;
	using System.Threading.Tasks;
	using Newtonsoft.Json.Linq;
	using Useful;

	/// <summary>
	/// A transaction is a set of commands treated as one atomic unit by Mega. The user communicates to Mega by executing transactions
	/// containing any number of commands. Upon successful execution, Mega will provide a result for every command in the transaction.
	/// </summary>
	/// <remarks>
	/// Use <see cref="Channel.CreateTransaction"/> to create transactions.
	/// </remarks>
	public sealed class Transaction
	{
		/// <summary>
		/// Adds a command to the transaction.
		/// The command may be any .NET object that can be JSON-serialized into the transaction request sent to Mega.
		/// </summary>
		public void AddCommand(object command)
		{
			AddCommand<object>(command, null);
		}

		/// <summary>
		/// Adds a command to the transaction, providing a handler that will be called with the result of the command.
		/// The command may be any .NET object that can be JSON-serialized into the transaction request sent to Mega.
		/// </summary>
		public void AddCommand<TResult>(object command, Action<TResult> resultHandler) where TResult : class
		{
			Argument.ValidateIsNotNull(command, "command");

			var qc = new QueuedCommand { Command = command };

			if (resultHandler != null)
			{
				qc.ResultType = typeof(TResult);
				qc.ResultHandler = r => resultHandler((TResult)r);
			}

			_commands.Add(qc);
		}

		/// <summary>
		/// Executes the transaction, returning the results for all performed commands.
		/// Any standalone result handlers for commands will be called before this function returns.
		/// </summary>
		public async Task<JArray> ExecuteAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (_hasBeenExecuted)
				throw new InvalidOperationException("The transaction has already been executed.");

			_hasBeenExecuted = true;

			var commandArray = _commands.Select(c => c.Command).ToArray();

			// Any failure will throw an AggregateException here, containing the exception that happened.
			var results = await _executor(commandArray, cancellationToken);

			if (results.Count != commandArray.Length)
				throw new ProtocolViolationException(string.Format("We executed {0} commands but only got back {1} results.", commandArray.Length, results.Count));

			// First, call all defined result handlers.
			for (int i = 0; i < results.Count; i++)
			{
				if (_commands[i].ResultHandler != null)
				{
					// Must convert result to desired data type, too.
					var result = results[i].ToObject(_commands[i].ResultType);

					_commands[i].ResultHandler(result);
				}
			}

			// And then return all the results.
			return results;
		}

		/// <summary>
		/// Creates a transaction, with a delegate that should be called to actually execute the transaction.
		/// This is intended to be used by Channel, so you should not use this directly.
		/// </summary>
		internal Transaction(Func<object[], CancellationToken, Task<JArray>> executor)
		{
			Argument.ValidateIsNotNull(executor, "executor");

			_executor = executor;
		}

		private bool _hasBeenExecuted;

		private readonly Func<object[], CancellationToken, Task<JArray>> _executor;

		private readonly List<QueuedCommand> _commands = new List<QueuedCommand>();

		private sealed class QueuedCommand
		{
			public object Command;

			public Type ResultType;
			public Action<object> ResultHandler;
		}
	}
}