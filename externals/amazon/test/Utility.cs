using System;
using System.IO;
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

	public static byte[] ComputeHash(string filePath, HashAlgorithm algorithm = null)
	{
		algorithm ??= SHA1.Create();
		using var stream = Zongsoft.IO.FileSystem.File.Open(filePath, FileMode.Open, FileAccess.Read);
		return algorithm.ComputeHash(stream);
	}
}
