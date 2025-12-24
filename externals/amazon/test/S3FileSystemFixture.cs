using System;

using Zongsoft.IO;
using Zongsoft.Configuration;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Externals.Amazon.Tests;

public class S3FileSystemFixture : IDisposable
{
	public IConfigurationManager Configuration { get; private set; }

	public S3FileSystemFixture()
	{
		this.Configuration = new ConfigurationManager();
		this.Configuration.AddOptionFile(@"Zongsoft.Externals.Amazon.option");
		FileSystem.Providers.Add(new IO.S3FileSystem(this.Configuration));
	}

	void IDisposable.Dispose() { GC.SuppressFinalize(this); }
}
