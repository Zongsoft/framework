using System;
using System.Threading.Tasks;

using Zongsoft.IO;
using Zongsoft.Configuration;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Externals.Amazon.Tests;

public class S3FileSystemFixture : IAsyncLifetime
{
	public IConfigurationManager Configuration { get; private set; }
	public string Root { get; }

	public S3FileSystemFixture()
	{
		if(!Global.IsTestingEnabled)
			return;

		var bucket = Environment.GetEnvironmentVariable("ZONGSOFT_AMAZON_TEST_BUCKET");
		if(string.IsNullOrWhiteSpace(bucket) || bucket.IndexOfAny(['/', '\\', '@', ':']) >= 0)
			throw new InvalidOperationException("Set ZONGSOFT_AMAZON_TEST_BUCKET to an isolated test bucket name.");

		this.Root = $"zfs.s3:/{bucket}/zongsoft-tests-{Guid.NewGuid():N}/";
		this.Configuration = new ConfigurationManager();
		this.Configuration.AddOptionFile(@"Zongsoft.Externals.Amazon.option");
		FileSystem.Providers.Add(new IO.S3FileSystem(this.Configuration));
	}

	public ValueTask InitializeAsync() => ValueTask.CompletedTask;

	public async ValueTask DisposeAsync()
	{
		if(this.Root != null)
		{
			try
			{
				await FileSystem.Directory.DeleteAsync(this.Root);
			}
			finally
			{
				FileSystem.Providers.Remove("zfs.s3");
			}
		}

		GC.SuppressFinalize(this);
	}
}
