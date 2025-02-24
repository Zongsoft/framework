using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Externals.ClosedXml.Tests.Models;

namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetExtractorTest
{
	private readonly SpreadsheetExtractor _extractor = new();
	private readonly SpreadsheetGenerator _generator = new();
	private readonly SpreadsheetRenderer _renderer = new();

    [Fact]
	public async Task TestExtractAsync()
	{
		using var stream = new MemoryStream();
		await _generator.GenerateAsync(stream, Templates.User.Descriptor, Templates.User.Data);
		stream.Seek(0, SeekOrigin.Begin);

		var result = _extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor)).Synchronize().ToArray();
		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(Templates.User.Data.Length, result.Length);

		for(var i = 0; i < result.Length; i++)
		{
			Assert.Equal(Templates.User.Data[i].UserId, result[i].UserId);
			Assert.Equal(Templates.User.Data[i].Name, result[i].Name);
			Assert.Equal(Templates.User.Data[i].Nickname, result[i].Nickname);
			Assert.Equal(Templates.User.Data[i].Gender, result[i].Gender);
			Assert.Equal(Templates.User.Data[i].Birthday, result[i].Birthday);
		}
	}

	[Fact]
	public async Task TestTemplateExtractAsync()
	{
		using var stream = new MemoryStream();
		await _renderer.RenderAsync(stream, Templates.ApartmentUsage.Template, Templates.ApartmentUsage);
		stream.Seek(0, SeekOrigin.Begin);

		var result = _extractor.ExtractAsync<AssetUsage>(stream, new DataArchiveExtractorOptions(Templates.AssetUsage.Descriptor))
			.Synchronize()
			.Where(usage => usage.AssetId != 0)
			.ToArray();

		Assert.NotNull(result);
		Assert.NotEmpty(result);
		Assert.Equal(Templates.ApartmentUsage.Usages.Length, result.Length);

		for(var i = 0; i < result.Length; i++)
		{
			Assert.Equal(Templates.ApartmentUsage.Usages[i].AssetId, result[i].AssetId);
		}
	}
}