using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Client;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal class Program
{
	private static OpcClient _client;

	static void Main(string[] args)
	{
		_client = new OpcClient();

		var executor = new TerminalCommandExecutor(ConsoleTerminal.Instance);

		executor.Command("info", Commands.Info, _client);
		executor.Command("exit", context => { _client.Dispose(); throw new TerminalCommandExecutor.ExitException(); });
		executor.Command("clear", context => context.Terminal.Clear());

		executor.Command("connect", (context, cancellation) =>
		{
			var url = context.Expression.Arguments.Length == 0 ? "opc.tcp://localhost:4841/OpcServer" : context.Expression.Arguments[0];
			return _client.ConnectAsync(url, cancellation);
		});

		executor.Command("disconnect", (context, cancellation) => _client.DisconnectAsync(cancellation));

		executor.Command("sub", (context, cancellation) =>
		{
			if(context.Expression.Arguments.Length > 0)
				return _client.SubscribeAsync(context.Expression.Arguments, null, cancellation);
			else
				return ValueTask.FromResult(false);
		});

		executor.Command("unsub", (context, cancellation) =>
		{
			if(context.Expression.Arguments.Length == 0)
				return _client.UnsubscribeAsync(cancellation);
			else
				return _client.UnsubscribeAsync(context.Expression.Arguments.AsSpan(1).ToArray(), cancellation);
		});

		executor.Command("exist", async (context, cancellation) =>
		{
			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				var existed = await _client.ExistsAsync(context.Expression.Arguments[i], cancellation);

				if(existed)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The '{context.Expression.Arguments[i]}' node exists.");
				else
					context.Output.WriteLine(CommandOutletColor.DarkRed, $"The '{context.Expression.Arguments[i]}' node was not found.");
			}

			return true;
		});

		executor.Command("get", async (context, cancellation) =>
		{
			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				var value = await _client.GetValueAsync(context.Expression.Arguments[i], cancellation);

				if(value != null)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, value);
				else
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The '{context.Expression.Arguments[i]}' node not found.");
			}
		});

		executor.Command("set", async (context, cancellation) =>
		{
			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				var succeed = await _client.SetValueAsync(context.Expression.Arguments[i], context.Expression.Arguments[++i], cancellation);

				if(succeed)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, "The set operation was successful.");
				else
					context.Output.WriteLine(CommandOutletColor.DarkRed, "The set operation failed.");
			}
		});

		executor.Command("folder", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.Length == 0)
				return;

			var created = await _client.CreateFolderAsync(context.Expression.Arguments[0], context.Expression.Arguments.Length > 1 ? context.Expression.Arguments[1] : null, cancellation);

			if(created)
				context.Output.WriteLine(CommandOutletColor.DarkGreen, "The folder was created successfully.");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "The folder creation failed.");
		});

		executor.Command("variable", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.Length == 0)
				return;

			var name = context.Expression.Arguments[0];
			var type = TypeAlias.Parse(context.Expression.Options.GetValue<string>("type"));
			var label = context.Expression.Options.GetValue("label", name);

			var created = await _client.CreateVariableAsync(name, type, label, context.Expression.Arguments.Length > 1 ? context.Expression.Arguments[1] : null, cancellation);

			if(created)
				context.Output.WriteLine(CommandOutletColor.DarkGreen, "The variable was created successfully.");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "The variable creation failed.");
		});

		executor.Run($"Welcome to the OPC-UA Client.{Environment.NewLine}{new string('-', 50)}");
	}
}
