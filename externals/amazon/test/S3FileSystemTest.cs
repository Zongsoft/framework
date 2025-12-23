using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Microsoft.Extensions.Configuration;

using Xunit;

using Zongsoft.IO;
using Zongsoft.Configuration;

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
	public async Task ExistsAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var existed = await FileSystem.File.ExistsAsync($"zfs.s3:/zongsoft-fs/NoExisted-{Guid.NewGuid()}.ext");
		Assert.False(existed);
	}

	[Fact]
	public async Task DeleteAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var existed = await FileSystem.File.DeleteAsync($"zfs.s3:/zongsoft-fs/NoExisted-{Guid.NewGuid()}.ext");
		Assert.False(existed);
	}

	[Fact]
	public async Task GetInfoAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var info = await FileSystem.File.GetInfoAsync($"zfs.s3:/zongsoft-fs/NoExisted-{Guid.NewGuid()}.ext");
		Assert.Null(info);
	}

	[Fact]
	public async Task SetInfoAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var tags = new Dictionary<string, object>
		{
			{ "Author", "Popeye Zhong" },
		};

		var result = await FileSystem.File.SetInfoAsync($"zfs.s3:/zongsoft-fs/NoExisted-{Guid.NewGuid()}.ext", tags);
		Assert.False(result);
	}

	[Fact]
	public async Task OpenAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		var properties = new Dictionary<string, object>()
		{
			{ "Author", "Popeye Zhong" },
		};

		var buffer = new byte[1024];
		var filePath = $"zfs.s3:/zongsoft-fs/${Zongsoft.Common.Randomizer.GenerateString()}.bin";

		using var source = new MemoryStream();
		using(var target = FileSystem.File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, properties))
		{
			Assert.NotNull(target);

			for(int i = 0; i < 512; i++)
			{
				Random.Shared.NextBytes(buffer);
				source.Write(buffer, 0, buffer.Length);
				await target.WriteAsync(buffer);
			}
		}

		Assert.True(FileSystem.File.Exists(filePath));
		var info = await FileSystem.File.GetInfoAsync(filePath);
		Assert.NotNull(info);
		Assert.Equal(source.Length, info.Size);
		Assert.True(info.HasProperties);

		foreach(var property in properties)
		{
			Assert.True(info.Properties.ContainsKey(property.Key));
			Assert.Equal(property.Value, info.Properties[property.Key]);
		}

		Assert.True(Enumerable.SequenceEqual(GetHash(source), GetHash(filePath)));
		Assert.True(FileSystem.File.Delete(filePath));
	}

	[Fact]
	public async Task AppendAsync()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int FIRST_LENGTH = 512;
		const int LAST_LENGTH = 1024;

		var properties = new Dictionary<string, object>()
		{
			{ "Author", "Popeye Zhong" },
		};

		var buffer = new byte[1024];
		var filePath = $"zfs.s3:/zongsoft-fs/${Zongsoft.Common.Randomizer.GenerateString()}.bin";

		using var first = new MemoryStream();
		using(var target = FileSystem.File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, properties))
		{
			Assert.NotNull(target);

			for(int i = 0; i < FIRST_LENGTH; i++)
			{
				Random.Shared.NextBytes(buffer);
				first.Write(buffer, 0, buffer.Length);
				await target.WriteAsync(buffer);
			}
		}

		Assert.True(Enumerable.SequenceEqual(GetHash(first), GetHash(filePath)));

		properties["Author"] = "Popeye";
		properties["Creation"] = DateTime.Now.ToString();

		using var last = new MemoryStream();
		using(var target = FileSystem.File.Open(filePath, FileMode.Append, FileAccess.ReadWrite, properties))
		{
			Assert.NotNull(target);

			for(int i = 0; i < LAST_LENGTH; i++)
			{
				Random.Shared.NextBytes(buffer);
				last.Write(buffer, 0, buffer.Length);
				await target.WriteAsync(buffer);
			}
		}

		Assert.True(FileSystem.File.Exists(filePath));
		var info = await FileSystem.File.GetInfoAsync(filePath);
		Assert.NotNull(info);
		Assert.Equal(first.Length + last.Length, info.Size);
		Assert.True(info.HasProperties);

		foreach(var property in properties)
		{
			Assert.True(info.Properties.ContainsKey(property.Key));
			Assert.Equal(property.Value, info.Properties[property.Key]);
		}

		buffer = new byte[first.Length + last.Length];
		Array.Copy(first.ToArray(), 0, buffer, 0, first.Length);
		Array.Copy(last.ToArray(), 0, buffer, first.Length, last.Length);
		Assert.True(Enumerable.SequenceEqual(GetHash(buffer), GetHash(filePath)));
		Assert.True(FileSystem.File.Delete(filePath));
	}


	private static byte[] GetHash(byte[] bytes, HashAlgorithm algorithm = null)
	{
		algorithm ??= SHA1.Create();
		return algorithm.ComputeHash(bytes);
	}

	private static byte[] GetHash(Stream stream, HashAlgorithm algorithm = null)
	{
		stream.Seek(0, SeekOrigin.Begin);
		algorithm ??= SHA1.Create();
		return algorithm.ComputeHash(stream);
	}

	private static byte[] GetHash(string filePath, HashAlgorithm algorithm = null)
	{
		algorithm ??= SHA1.Create();
		using var stream = FileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read);
		return algorithm.ComputeHash(stream);
	}
}
