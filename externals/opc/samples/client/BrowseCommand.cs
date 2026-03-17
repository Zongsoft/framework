using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

[CommandOption(INCLUDE_BUILTINS_OPTION, 'b', typeof(bool))]
[CommandOption(INCLUDE_SUBTYPES_OPTION, 's', typeof(bool))]
[CommandOption(HIERARCHICALLY_OPTION, 'h', typeof(bool), DefaultValue = true)]
[CommandOption(KIND_OPTION, 'k', typeof(OpcNodeKind), DefaultValue = OpcNodeKind.Object | OpcNodeKind.Variable)]
internal sealed class BrowseCommand(OpcClient client) : CommandBase<CommandContext>("Browse")
{
	#region 常量定义
	private const string KIND_OPTION = "kind";
	private const string HIERARCHICALLY_OPTION = "hierarchically";
	private const string INCLUDE_BUILTINS_OPTION = "include-builtins";
	private const string INCLUDE_SUBTYPES_OPTION = "include-subtypes";
	#endregion

	private readonly OpcClient _client = client ?? throw new ArgumentNullException(nameof(client));

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		IAsyncEnumerable<OpcNode>[] result;
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();

		var options = new OpcClient.BrowseOptions(
			context.Options.GetValue<OpcNodeKind>(KIND_OPTION),
			context.Options.Switch(HIERARCHICALLY_OPTION))
		{
			IncludeBuiltins = context.Options.Switch(INCLUDE_BUILTINS_OPTION),
			IncludeSubtypes = context.Options.Switch(INCLUDE_SUBTYPES_OPTION),
		};

		if(context.Arguments.IsEmpty)
			result = [_client.BrowseAsync(options, cancellation)];
		else if(context.Arguments.Count == 1)
			result = [_client.BrowseAsync(context.Arguments[0], options, cancellation)];
		else
		{
			result = new IAsyncEnumerable<OpcNode>[context.Arguments.Count];

			for(int i = 0; i < context.Arguments.Count; i++)
				result[i] = _client.BrowseAsync(context.Arguments[i], options, cancellation);
		}

		var total = 0;

		for(int i = 0; i < result.Length; i++)
		{
			if(result[i] == null)
				continue;

			#if NET10_0_OR_GREATER
			var nodes = result[i].OrderBy(node => node.Name);
			#else
			var nodes = result[i];
			#endif

			var count = 0;
			await foreach(var node in nodes)
			{
				++count;
				Dump(context.Output, node);
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

	private static void Dump(ICommandOutlet output, OpcNode node, int depth = 0)
	{
		output.WriteLine(ToContent(node, depth));

		if(node.HasChildren(out var children))
		{
			foreach(var child in children)
				Dump(output, child, depth + 1);
		}

		static CommandOutletContent ToContent(OpcNode node, int depth = 0)
		{
			if(node == null)
				return null;

			var content = CommandOutletContent.Create(depth > 0 ? new string('\t', depth) : string.Empty)
				.Append(CommandOutletColor.DarkCyan, $"[{node.Kind}]")
				.Append(CommandOutletColor.DarkGreen, $" {node.Name}");

			if(!string.IsNullOrEmpty(node.Label))
				content.Last.Append(CommandOutletColor.DarkGray, $" {node.Label}");

			if(node.Type != null)
				content.Last
					.Append(CommandOutletColor.DarkCyan, '(')
					.Append(CommandOutletColor.DarkYellow, node.Type)
					.Append(CommandOutletColor.DarkCyan, ')');

			if(!string.IsNullOrEmpty(node.Description) && !string.Equals(node.Label, node.Description))
				content.Last.Append(CommandOutletColor.DarkGray, $" {node.Description}");

			return content;
		}
	}
}