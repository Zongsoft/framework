using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Plugins.Tests;

public class PredicateParserTests
{
	[Theory]
	[InlineData(false, "allowed")]
	[InlineData(true, "allowed")]
	[InlineData(false, "denied")]
	[InlineData(true, null)]
	public void TypedPredicationReceivesPluginContextAndAwaitsCompletion(bool asynchronous, string parameter)
	{
		var predication = new ContextPredication(asynchronous);
		using var scope = new PluginTestScope(configure: services => services.AddSingleton<IPredication>(predication));
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Anchor\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Anchor");

		var result = Parser.Parse($"{{predicate:Context {parameter}}}", builtin, null, null);

		Assert.Equal(parameter == "allowed", Assert.IsType<bool>(result));
		Assert.Equal(parameter, predication.Context.Parameter);
		Assert.Same(builtin, predication.Context.Builtin);
		Assert.Same(builtin.Node, predication.Context.Node);
		Assert.Same(builtin.Plugin, predication.Context.Plugin);
		Assert.False(builtin.IsBuilded);
	}

	[Fact]
	public void ModulePredicationReceivesRawArgumentsAndDoesNotFallBackToGlobalServices()
	{
		var global = new TextPredication();
		var local = new TextPredication();
		using var scope = new PluginTestScope(configure: services => services.AddSingleton<IPredication>(global));
		using var moduleServices = new ServiceCollection().AddSingleton<IPredication>(local).BuildServiceProvider();
		scope.Application.Modules.Add(new TestModule(moduleServices));
		scope.Tree.Load();
		var node = scope.Tree.EnsurePath("/Items");

		Assert.Equal(true, Parser.Parse("{predicate:Orders.Text two  words}", node, null, null));
		Assert.Equal("two  words", local.Argument);
		Assert.Null(global.Argument);
		Assert.Equal(false, Parser.Parse("{predicate:Orders.Absent}", node, null, null));
		Assert.Equal(false, Parser.Parse("{predicate:Absent}", node, null, null));
		Assert.Throws<PluginException>(() => Parser.Parse("{predicate:Missing.Text}", node, null, null));
		Assert.Throws<PluginException>(() => Parser.Parse("{predicate:Orders.Text.Extra}", node, null, null));
	}

	private sealed class ContextPredication(bool asynchronous) : PredicationBase<PluginPredicationContext>("Context")
	{
		public PluginPredicationContext Context { get; private set; }

		public override async ValueTask<bool> PredicateAsync(PluginPredicationContext argument, Collections.Parameters parameters, CancellationToken cancellation = default)
		{
			if(asynchronous)
				await Task.Delay(10, cancellation).ConfigureAwait(false);

			this.Context = argument;
			return argument.Parameter == "allowed";
		}
	}

	private sealed class TextPredication() : PredicationBase<string>("Text")
	{
		public string Argument { get; private set; }
		public override ValueTask<bool> PredicateAsync(string argument, Collections.Parameters parameters, CancellationToken cancellation = default)
		{
			this.Argument = argument;
			return ValueTask.FromResult(argument != null);
		}
	}

	private sealed class TestModule(IServiceProvider services) : ApplicationModule("Orders")
	{
		public override IServiceProvider Services => services;
	}
}
