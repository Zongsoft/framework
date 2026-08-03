using Zongsoft.Externals.ClosedXml.Tests.Models;

namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetExtractorTest
{
	private readonly SpreadsheetExtractor _extractor = new();
	private readonly SpreadsheetGenerator _generator = new();

	[Fact]
	public async Task ExtractAsync_GeneratedWorkbook_RestoresTypedRecords()
	{
		using var stream = new MemoryStream();
		await _generator.GenerateAsync(stream, Templates.User.Descriptor, Templates.User.Data);
		stream.Position = 0;

		var result = _extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor))
			.Synchronize()
			.ToArray();

		Assert.Equal(Templates.User.Data.Length, result.Length);
		for(var index = 0; index < result.Length; index++)
		{
			Assert.Equal(Templates.User.Data[index].UserId, result[index].UserId);
			Assert.Equal(Templates.User.Data[index].Name, result[index].Name);
			Assert.Equal(Templates.User.Data[index].Nickname, result[index].Nickname);
			Assert.Equal(Templates.User.Data[index].Email, result[index].Email);
			Assert.Equal(Templates.User.Data[index].Phone, result[index].Phone);
			Assert.Equal(Templates.User.Data[index].Gender, result[index].Gender);
			Assert.Equal(Templates.User.Data[index].Birthday, result[index].Birthday);
			Assert.Equal(Templates.User.Data[index].Description, result[index].Description);
		}
	}

	[Fact]
	public void ExtractAsync_EmptyRowsAndAlternateWorksheet_SkipsEmptyRowsAndUsesRequestedSheet()
	{
		using var stream = CreateImportWorkbook();
		var options = new DataArchiveExtractorOptions(Templates.User.Descriptor) { Source = "Import" };

		var result = _extractor.ExtractAsync<User>(stream, options).Synchronize().ToArray();

		Assert.Collection(result,
			first =>
			{
				Assert.Equal(701, first.UserId);
				Assert.Equal("Grace", first.Name);
				Assert.Equal(new DateTime(1906, 12, 9), first.Birthday);
			},
			second =>
			{
				Assert.Equal(702, second.UserId);
				Assert.Equal("Linus", second.Name);
				Assert.Null(second.Birthday);
			});
	}

	[Fact]
	public void ExtractAsync_MissingModelRange_ReturnsEmpty()
	{
		using var workbook = new XLWorkbook();
		workbook.AddWorksheet("Users").Cell("A1").SetValue("not a named data range");
		using var stream = new MemoryStream();
		workbook.SaveAs(stream);
		stream.Position = 0;

		var result = _extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor))
			.Synchronize()
			.ToArray();

		Assert.Empty(result);
	}

	[Fact]
	public void ExtractAsync_NullInput_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() => _extractor.ExtractAsync<User>(null, new DataArchiveExtractorOptions(Templates.User.Descriptor)));
	}

	[Fact]
	public void ExtractAsync_NullOptions_ThrowsArgumentNullException()
	{
		using var stream = CreateImportWorkbook();
		Assert.Throws<ArgumentNullException>(() => _extractor.ExtractAsync<User>(stream, null));
	}

	private static MemoryStream CreateImportWorkbook()
	{
		using var workbook = new XLWorkbook();
		workbook.AddWorksheet("Ignored").Cell("A1").SetValue("wrong worksheet");
		var worksheet = workbook.AddWorksheet("Import");
		worksheet.Cell("A1").SetValue(nameof(User.UserId)).AddToNamed(nameof(User.UserId), XLScope.Worksheet);
		worksheet.Cell("B1").SetValue(nameof(User.Name)).AddToNamed(nameof(User.Name), XLScope.Worksheet);
		worksheet.Cell("C1").SetValue(nameof(User.Birthday)).AddToNamed(nameof(User.Birthday), XLScope.Worksheet);
		worksheet.Cell("A2").SetValue(701);
		worksheet.Cell("B2").SetValue("Grace");
		worksheet.Cell("C2").SetValue(new DateTime(1906, 12, 9));
		worksheet.Cell("A4").SetValue(702);
		worksheet.Cell("B4").SetValue("Linus");
		worksheet.Range("A2:C4").AddToNamed(Templates.User.Descriptor.Name, XLScope.Workbook);

		var stream = new MemoryStream();
		workbook.SaveAs(stream);
		stream.Position = 0;
		return stream;
	}
}
