using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Amazon.Tests;

internal static class Utility
{
	public static byte[] ComputeHash(byte[] bytes, HashAlgorithm algorithm = null)
	{
		algorithm ??= SHA1.Create();
		return algorithm.ComputeHash(bytes);
	}

	public static byte[] ComputeHash(Stream stream, HashAlgorithm algorithm = null)
	{
		algorithm ??= SHA1.Create();
		stream.Seek(0, SeekOrigin.Begin);
		return algorithm.ComputeHash(stream);
	}

	public static async Task<byte[]> ComputeHashAsync(string filePath)
	{
		using var algorithm = SHA1.Create();
		await using var stream = await Zongsoft.IO.FileSystem.File.OpenAsync(filePath, FileMode.Open, FileAccess.Read);
		return await algorithm.ComputeHashAsync(stream);
	}

	public static async Task<T[]> ToArrayAsync<T>(IAsyncEnumerable<T> source)
	{
		var items = new List<T>();

		await foreach(var item in source)
			items.Add(item);

		return items.ToArray();
	}
}
