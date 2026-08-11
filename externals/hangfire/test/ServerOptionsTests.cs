using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Externals.Hangfire.Tests;

public class ServerOptionsTests
{
	[Theory]
	[InlineData("critical, default")]
	[InlineData("critical; default")]
	public void Queues_DelimitedConfiguration_BindsAllQueues(string queues)
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string>
			{
				[nameof(ServerOptions.Queues)] = queues,
			})
			.Build();
		var options = new ServerOptions();

		Zongsoft.Configuration.ConfigurationBinder.Bind(configuration, options);

		Assert.Equal(["critical", "default"], options.Queues);
	}
}
