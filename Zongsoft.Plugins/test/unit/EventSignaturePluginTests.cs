using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Plugins.Tests;

public class EventSignaturePluginTests
{
	public static readonly EventDescriptor Created = new("Building.Created", [typeof(string), typeof(IEnumerable<string>)]);

	public static readonly EventDescriptor<string> Updated = new("Building.Updated");

	[Fact]
	public async Task SingleAndBatchHandlersShareOneExposedEventPath()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Events/Building">
				<expose name="Created" value="{static:Zongsoft.Plugins.Tests.EventSignaturePluginTests.Created, Zongsoft.Plugins.Tests}" />
			</extension>
			<extension path="/Events/Building/Created">
				<object name="Single" type="{{typeof(SingleHandler).AssemblyQualifiedName}}" />
				<object name="Batch" type="{{typeof(BatchHandler).AssemblyQualifiedName}}" />
			</extension>
			""");

		try
		{
			scope.Tree.Load();
			var descriptor = Assert.IsType<EventDescriptor>(scope.Tree.Find("/Events/Building/Created").UnwrapValue(ObtainMode.Auto));
			Assert.Same(Created, descriptor);
			var single = Assert.Single(descriptor.Handlers.OfType<SingleHandler>());
			var batch = Assert.Single(descriptor.Handlers.OfType<BatchHandler>());
			Assert.Equal(2, descriptor.Handlers.Count);
			await descriptor.HandleAsync("one");
			Assert.Equal(1, single.Count);
			Assert.Equal(0, batch.Count);
			await descriptor.HandleAsync<IEnumerable<string>>(["one", "two"]);
			Assert.Equal(1, single.Count);
			Assert.Equal(1, batch.Count);
			Assert.Equal(["one", "two"], batch.Values);
		}
		finally { Created.Handlers.Clear(); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task BothDeclarationFormsExposeTheirHandlerCollection(bool generic)
	{
		var name = generic ? nameof(Updated) : nameof(Created);
		var expected = generic ? Updated : Created;
		await using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Events/Building">
				<expose name="{{name}}" value="{static:Zongsoft.Plugins.Tests.EventSignaturePluginTests.{{name}}, Zongsoft.Plugins.Tests}" />
			</extension>
			<extension path="/Events/Building/{{name}}">
				<object name="Single" type="{{typeof(SingleHandler).AssemblyQualifiedName}}" />
			</extension>
			""");

		try
		{
			scope.Tree.Load();
			var descriptor = Assert.IsAssignableFrom<EventDescriptor>(scope.Tree.Find($"/Events/Building/{name}").UnwrapValue(ObtainMode.Auto));
			Assert.Same(expected, descriptor);
			var handler = Assert.IsType<SingleHandler>(Assert.Single(descriptor.Handlers));
			await descriptor.HandleAsync("one");
			Assert.Equal(1, handler.Count);
		}
		finally { expected.Handlers.Clear(); }
	}

	public sealed class SingleHandler : HandlerBase<string>
	{
		public int Count { get; private set; }
		protected override ValueTask OnHandleAsync(string argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Count++;
			return ValueTask.CompletedTask;
		}
	}

	public sealed class BatchHandler : HandlerBase<IEnumerable<string>>
	{
		public int Count { get; private set; }
		public string[] Values { get; private set; }
		protected override ValueTask OnHandleAsync(IEnumerable<string> argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Count++;
			this.Values = argument.ToArray();
			return ValueTask.CompletedTask;
		}
	}
}
