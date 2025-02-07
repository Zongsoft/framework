using System;

using Xunit;

namespace Zongsoft.IO.Compression.Tests;

public class CompressorTest
{
	private readonly byte[] _data;

	public CompressorTest()
	{
		_data = GetData();
	}

	[Fact]
	public void TestBrotli()
	{
		var compressed = Compressor.Compress("br", _data);
		Assert.NotNull(compressed);
		Assert.NotEmpty(compressed);

		var result = Compressor.Decompress("Brotli", compressed);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(_data.Length, result.Length);
		Assert.Equal(_data, result);
	}

	[Fact]
	public void TestGZip()
	{
		var compressed = Compressor.Compress("gzip", _data);
		Assert.NotNull(compressed);
		Assert.NotEmpty(compressed);

		var result = Compressor.Decompress("GZip", compressed);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(_data.Length, result.Length);
		Assert.Equal(_data, result);
	}

	[Fact]
	public void TestZLib()
	{
		var compressed = Compressor.Compress("zlib", _data);
		Assert.NotNull(compressed);
		Assert.NotEmpty(compressed);

		var result = Compressor.Decompress("Zlib", compressed);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(_data.Length, result.Length);
		Assert.Equal(_data, result);
	}

	[Fact]
	public void TestDeflate()
	{
		var compressed = Compressor.Compress("deflate", _data);
		Assert.NotNull(compressed);
		Assert.NotEmpty(compressed);

		var result = Compressor.Decompress("Deflate", compressed);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(_data.Length, result.Length);
		Assert.Equal(_data, result);
	}

	private static byte[] GetData(int count = 1024 * 10)
	{
		var data = new byte[count];
		Random.Shared.NextBytes(data);
		return data;
	}
}
