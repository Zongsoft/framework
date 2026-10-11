using System;
using System.IO;
using System.Collections;

using Xunit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Tests;

public class ExposeBuilderTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ExposedChildPreservesExistingAppendingBehavior(bool buildChildFirst)
	{
		var directory = Path.Combine(Path.GetTempPath(), "zongsoft-plugins-" + Guid.NewGuid().ToString("N"));
		var plugins = Path.Combine(directory, "plugins");
		Directory.CreateDirectory(plugins);
		using var application = new TestApplicationContext(new Services.ServiceProviderFactory().CreateServiceProvider(new ServiceCollection()));

		try
		{
			File.WriteAllText(Path.Combine(plugins, "Main.plugin"), """
				<plugin name="Main">
					<builders>
						<builder name="object" type="Zongsoft.Plugins.Builders.ObjectBuilder, Zongsoft.Plugins" />
						<builder name="expose" type="Zongsoft.Plugins.Builders.ExposeBuilder, Zongsoft.Plugins" />
					</builders>
					<extension path="/Fixture">
						<object name="Container" type="System.Collections.ArrayList">
							<object name="Regular" type="System.Object" />
						</object>
					</extension>
					<extension path="/Fixture/Container/References">
						<expose name="Shared" type="System.Object" />
					</extension>
				</plugin>
				""");

			var tree = PluginTree.Get(new PluginOptions(new ConfigurationBuilder().Build(), directory, "development")).Load();
			var child = tree.Find("/Fixture/Container/References/Shared");

			if(buildChildFirst)
				child.UnwrapValue(ObtainMode.Auto);

			var container = Assert.IsType<ArrayList>(tree.Find("/Fixture/Container").UnwrapValue(ObtainMode.Auto));
			var regular = tree.Find("/Fixture/Container/Regular").UnwrapValue(ObtainMode.Never);
			var exposed = child.UnwrapValue(ObtainMode.Never);

			Assert.NotNull(exposed);
			Assert.Same(regular, container[0]);

			//兼容性基线：首次构建不追加，缓存子节点沿用原有追加行为。
			if(buildChildFirst)
			{
				Assert.Equal(2, container.Count);
				Assert.Same(exposed, container[1]);
			}
			else
			{
				Assert.Single(container);
				Assert.False(container.Contains(exposed));
			}
		}
		finally
		{
			Directory.Delete(directory, true);
		}
	}

	private sealed class TestApplicationContext(IServiceProvider services) : Services.ApplicationContext(services);
}
