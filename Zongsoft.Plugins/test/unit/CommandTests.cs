using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Plugins.Commands;
using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Plugins.Tests;

public class CommandTests
{
	[Fact]
	public async Task FindReturnsValuesAndOnlyBuildsWhenRequestedAsync()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Deferred\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var first = new object();
		var second = new object();
		scope.Tree.Mount("/Items/First", first);
		scope.Tree.Mount("/Items/Second", second);
		var executor = CreateExecutor(scope.Tree);
		var builtin = scope.Builtin("/Items/Deferred");

		Assert.Null(await executor.ExecuteAsync("find /Items/Deferred"));
		Assert.False(builtin.IsBuilded);
		Assert.Null(await executor.ExecuteAsync("find /Missing"));
		Assert.Same(first, await executor.ExecuteAsync("find /Items/First"));
		Assert.Equal(new[] { first, second }, Assert.IsType<List<object>>(await executor.ExecuteAsync("find /Items/First /Items/Second")));

		var built = await executor.ExecuteAsync("find --obtain:Auto /Items/Deferred");

		Assert.True(builtin.IsBuilded);
		Assert.Same(builtin.Value, built);
		Assert.NotSame(built, await executor.ExecuteAsync("find --obtain:Alway /Items/Deferred"));
	}

	[Fact]
	public async Task InspectingNodesIncludesMetadataAndChildrenWithoutConstructingThemAsync()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/\"><Items.FixtureMetadata>fixture-value</Items.FixtureMetadata></extension><extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"Child\" type=\"System.Object\" /></object></extension>");
		scope.Tree.Load();
		scope.Tree.EnsurePath("/Items/Empty");
		var executor = CreateExecutor(scope.Tree);
		using var output = new StringWriter();
		executor.Output = new RecordingOutlet(output);

		Assert.Null(await executor.ExecuteAsync("find /Items"));

		Assert.False(scope.Builtin("/Items/Parent").IsBuilded);
		Assert.False(scope.Builtin("/Items/Parent/Child").IsBuilded);
		Assert.Contains("FixtureMetadata", output.ToString());
		Assert.Contains("fixture-value", output.ToString());
		Assert.Contains("Parent", output.ToString());
		Assert.Contains("Empty", output.ToString());
	}

	[Fact]
	public async Task ListReturnsHierarchyAndTreeAcceptsPathsOrPipelineValuesAsync()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Deferred\" type=\"System.Object\" /></extension>");
		scope.WriteFile("Feature/Child.plugin", "<plugin name=\"Child\" version=\"1.2\" />");
		scope.Tree.Load();
		var executor = CreateExecutor(scope.Tree);
		var node = scope.Tree.Find("/Items");
		using var output = new StringWriter();
		executor.Output = new RecordingOutlet(output);

		Assert.Same(scope.Tree.Plugins, await executor.ExecuteAsync("list"));
		Assert.Contains("Feature.plugin", output.ToString());
		Assert.Contains("Child.plugin", output.ToString());
		output.GetStringBuilder().Clear();
		Assert.Same(node, await executor.ExecuteAsync("tree /Items"));
		Assert.Contains("Deferred", output.ToString());
		Assert.Same(node, await executor.ExecuteAsync("tree --depth:1", node));
		Assert.Null(await executor.ExecuteAsync("tree /Missing"));
		Assert.False(scope.Builtin("/Items/Deferred").IsBuilded);
	}

	[Theory]
	[InlineData("find", null)]
	[InlineData("tree", null)]
	[InlineData("tree /One /Two", null)]
	[InlineData("tree", "invalid")]
	public async Task MissingOrInvalidCommandArgumentsAreRejectedAsync(string expression, object value)
	{
		await using var scope = new PluginTestScope();
		var executor = CreateExecutor(scope.Tree);

		await Assert.ThrowsAsync<CommandException>(async () => await executor.ExecuteAsync(expression, value));
	}

	[Fact]
	public async Task CommandExpressionDefersExecutionAndForwardsParameterAndCancellationAsync()
	{
		await using var scope = new PluginTestScope();
		scope.Tree.Load();
		var echo = new EchoCommand();
		var executor = new CommandExecutor();
		executor.Root.Children.Add(echo);
		var previous = CommandExecutor.Default;
		CommandExecutor.Default = executor;

		try
		{
			var command = Assert.IsAssignableFrom<ICommand>(Parser.Parse("{command:echo fixture}", scope.Tree.EnsurePath("/Commands"), null, null));
			Assert.Equal(0, echo.Calls);
			using var cancellation = new CancellationTokenSource();
			var parameter = new object();

			var result = await command.ExecuteAsync(parameter, cancellation.Token);

			Assert.Same(parameter, result);
			Assert.Equal(1, echo.Calls);
			Assert.Equal("fixture", echo.Argument);
			Assert.Equal(cancellation.Token, echo.Cancellation);
		}
		finally
		{
			CommandExecutor.Default = previous;
		}
	}

	private static CommandExecutor CreateExecutor(PluginTree tree)
	{
		var executor = new CommandExecutor { Error = System.IO.TextWriter.Null };
		executor.Root.Children.Add(new FindCommand(tree));
		executor.Root.Children.Add(new ListCommand(tree));
		executor.Root.Children.Add(new TreeCommand(tree));
		return executor;
	}

	private sealed class EchoCommand() : CommandBase<CommandContext>("Echo")
	{
		public int Calls { get; private set; }
		public string Argument { get; private set; }
		public CancellationToken Cancellation { get; private set; }

		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			this.Calls++;
			this.Argument = context.Arguments[0];
			this.Cancellation = cancellation;
			return ValueTask.FromResult(context.Value);
		}
	}

	private sealed class RecordingOutlet(TextWriter writer) : ICommandOutlet
	{
		public Encoding Encoding { get; set; } = Encoding.UTF8;
		public TextWriter Writer => writer;
		public void Write(CommandOutletContent content) => writer.Write(CommandOutletContent.GetFullText(content));
		public void Write<T>(T value) => writer.Write(value);
		public void Write<T>(CommandOutletColor foregroundColor, T value) => this.Write(value);
		public void Write<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.Write(value);
		public void Write<T>(CommandOutletStyles style, T value) => this.Write(value);
		public void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => this.Write(value);
		public void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.Write(value);
		public void WriteLine() => writer.WriteLine();
		public void WriteLine(CommandOutletContent content) => writer.WriteLine(CommandOutletContent.GetFullText(content));
		public void WriteLine<T>(T value) => writer.WriteLine(value);
		public void WriteLine<T>(CommandOutletColor foregroundColor, T value) => this.WriteLine(value);
		public void WriteLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.WriteLine(value);
		public void WriteLine<T>(CommandOutletStyles style, T value) => this.WriteLine(value);
		public void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value) => this.WriteLine(value);
		public void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value) => this.WriteLine(value);
	}
}
