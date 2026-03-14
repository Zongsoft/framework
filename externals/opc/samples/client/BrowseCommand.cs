using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal sealed class BrowseCommand(OpcClient client) : CommandBase<CommandContext>("Browse")
{
	private readonly OpcClient _client = client ?? throw new ArgumentNullException(nameof(client));

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		IAsyncEnumerable<OpcNode>[] result;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		if(context.Arguments.IsEmpty)
			result = [_client.BrowseAsync(cancellation)];
		else if(context.Arguments.Count == 1)
			result = [_client.BrowseAsync(context.Arguments[0], cancellation)];
		else
		{
			result = new IAsyncEnumerable<OpcNode>[context.Arguments.Count];

			for(int i = 0; i < context.Arguments.Count; i++)
				result[i] = _client.BrowseAsync(context.Arguments[i], cancellation);
		}

		var total = 0;

		for(int i = 0; i < result.Length; i++)
		{
			if(result[i] == null)
				continue;

			var count = 0;
			await foreach(var node in result[i])
			{
				++count;

				if(!node.IsBuiltin)
					context.Output.WriteLine(Dump(node));
			}

			total += count;
			stopwatch.Stop();
			context.Output.WriteLine(CommandOutletContent.Create(new string('_', 45) + '\n')
				.Append(CommandOutletColor.DarkMagenta, $"[{i + 1}] ")
				.Append(CommandOutletColor.DarkGreen, " Total:")
				.Append(CommandOutletColor.DarkYellow, count.ToString("#,###0"))
				.Append(" ")
				.Append(CommandOutletColor.DarkGreen, " Elapsed:")
				.Append(CommandOutletColor.DarkYellow, stopwatch.Elapsed));
		}

		if(result == null || result.Length == 0)
			return null;

		return result.Length == 1 ? result[0] : result;
	}

	private static CommandOutletContent Dump(OpcNode node)
	{
		if(node == null)
			return null;

		var content = CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkCyan, $"[{node.Kind}]")
			.Append(CommandOutletColor.DarkGreen, $" {node.Name}");

		if(!string.IsNullOrEmpty(node.Label))
			content.Last.Append(CommandOutletColor.DarkGray, $" {node.Label}");

		content.Last
			.Append(CommandOutletColor.DarkCyan, '(')
			.Append(CommandOutletColor.DarkYellow, node.Type)
			.Append(CommandOutletColor.DarkCyan, ')');

		if(!string.IsNullOrEmpty(node.Description) && !string.Equals(node.Label, node.Description))
			content.Last.Append(CommandOutletColor.DarkGray, $" {node.Description}");

		return content;
	}
}