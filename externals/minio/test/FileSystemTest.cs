using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.IO;
using Zongsoft.Configuration;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Externals.MinIO.Tests;

public class FileSystemTest
{
	private readonly IConfigurationManager _configuration;

	public FileSystemTest()
	{
		_configuration = new ConfigurationManager();
		_configuration.AddOptionFile(@"Zongsoft.Externals.MinIO.option");
		FileSystem.Providers.Add(new MinIOFileSystem(_configuration));
	}

	[Fact]
	public async Task ExistsAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var existed = await FileSystem.File.ExistsAsync($"zfs.minio:/zongsoft-fs/NoExisted-{Guid.NewGuid()}.ext");
		Assert.False(existed);
	}

	[Fact]
	public async Task GetInfoAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var info = await FileSystem.File.GetInfoAsync(@"zfs.minio:/zongsoft-fs/美女.jpg");
		Assert.NotNull(info);
	}

	[Fact]
	public void Open()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var stream = FileSystem.File.Open($"zfs.minio:/zongsoft-fs/{Zongsoft.Common.Randomizer.GenerateString()}.txt", FileMode.Open, FileAccess.ReadWrite);
		Assert.NotNull(stream);

		for(int i = 0; i < 2; i++)
		{
			stream.Write(System.Text.Encoding.UTF8.GetBytes(Zongsoft.Common.Randomizer.GenerateString(1024)));
		}
	}
}
