using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.IO;
using Zongsoft.Configuration;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Externals.Amazon.Tests;

public class S3FileSystemTest
{
	private readonly IConfigurationManager _configuration;

	public S3FileSystemTest()
	{
		_configuration = new ConfigurationManager();
		_configuration.AddOptionFile(@"Zongsoft.Externals.Amazon.option");
		FileSystem.Providers.Add(new Storages.S3FileSystem(_configuration));
	}

	[Fact]
	public async Task GetInfoAsync()
	{
		var info = await FileSystem.File.GetInfoAsync(@"zfs.s3:/zongsoft-fs/美女.jpg");
		Assert.NotNull(info);
	}
}
