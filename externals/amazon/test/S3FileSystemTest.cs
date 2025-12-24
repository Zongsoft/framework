using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.IO;

namespace Zongsoft.Externals.Amazon.Tests;

public class S3FileSystemTest(S3FileSystemFixture fixture) : IClassFixture<S3FileSystemFixture>
{
	private readonly S3FileSystemFixture _fixture = fixture;

	[Fact]
	public void GetUrl()
	{
		if(!Global.IsTestingEnabled)
			return;

		var url = FileSystem.GetUrl("zfs.s3:/zongsoft-fs/Wukong.png");
		Assert.NotNull(url);
		Assert.NotEmpty(url);
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

		var result = await FileSystem.File.DeleteAsync($"zfs.s3:/zongsoft-fs/NoExisted-{Guid.NewGuid()}.ext");
		Assert.False(result);

		result = await FileSystem.Directory.DeleteAsync($"zfs.s3:/zongsoft-fs/videos/");
		Assert.False(result);
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
	public async Task GetChildrenAsync()
	{
		const int COUNT = 10;
		const string DIRECTORY = "zfs.s3:/zongsoft-fs/Directory";

		if(!Global.IsTestingEnabled)
			return;

		var buffer = new byte[1024];
		var children = FileSystem.Directory.GetChildrenAsync($"zfs.s3:/zongsoft-fs/NoExisted-{Guid.NewGuid()}/").ToBlockingEnumerable().ToArray();
		Assert.Empty(children);

		for(int i = 0; i < COUNT; i++)
		{
			using(var stream = FileSystem.File.Open($"{DIRECTORY}/File-{i:00000}.ext"))
			{
				Random.Shared.NextBytes(buffer);
				stream.Write(buffer);
			}
		}

		Assert.True(FileSystem.Directory.Exists(DIRECTORY));
		children = FileSystem.Directory.GetChildrenAsync(DIRECTORY)
			.ToBlockingEnumerable()
			.OrderBy(info => info.Name)
			.ToArray();

		Assert.NotEmpty(children);
		Assert.Equal(COUNT, children.Length);

		for(int i = 0; i < COUNT; i++)
		{
			Assert.Equal($"File-{i:00000}.ext", children[i].Name);
		}
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

		Assert.True(Enumerable.SequenceEqual(Utility.ComputeHash(source), Utility.ComputeHash(filePath)));
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

		Assert.True(Enumerable.SequenceEqual(Utility.ComputeHash(first), Utility.ComputeHash(filePath)));

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
		Assert.True(Enumerable.SequenceEqual(Utility.ComputeHash(buffer), Utility.ComputeHash(filePath)));
		Assert.True(FileSystem.File.Delete(filePath));
	}
}
