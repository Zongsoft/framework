using Zongsoft.Externals.ClosedXml.Tests.Models;

namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetGeneratorTest
{
	private readonly SpreadsheetGenerator _generator = new();

	[Fact]
	public async Task GenerateAsync_Records_CreatesTypedWorkbookAndNamedRanges()
	{
		using var output = new MemoryStream();

		await _generator.GenerateAsync(output, Templates.User.Descriptor, Templates.User.Data);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var worksheet = Assert.Single(workbook.Worksheets);
		var dataRange = Assert.Single(worksheet.DefinedName(Templates.User.Descriptor.Name).Ranges);

		Assert.Equal(4, dataRange.RangeAddress.FirstAddress.RowNumber);
		Assert.Equal(Templates.User.Data.Length, dataRange.RowCount());
		Assert.Equal(Templates.User.Descriptor.Properties.Count, dataRange.ColumnCount());
		Assert.Equal(XLPageOrientation.Landscape, worksheet.PageSetup.PageOrientation);
		Assert.True(worksheet.Range(1, 1, 1, dataRange.ColumnCount()).IsMerged());
		Assert.True(worksheet.Range(2, 1, 2, dataRange.ColumnCount()).IsMerged());

		var userIdColumn = GetColumnNumber(worksheet, nameof(User.UserId));
		var nameColumn = GetColumnNumber(worksheet, nameof(User.Name));
		var genderColumn = GetColumnNumber(worksheet, nameof(User.Gender));
		var birthdayColumn = GetColumnNumber(worksheet, nameof(User.Birthday));
		var emailColumn = GetColumnNumber(worksheet, nameof(User.Email));

		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.UserId)].Label, worksheet.Cell(3, userIdColumn).GetString());
		Assert.Equal(101, worksheet.Cell(4, userIdColumn).GetValue<int>());
		Assert.Equal("Popeye", worksheet.Cell(4, nameColumn).GetString());
		Assert.Equal(nameof(Gender.Male), worksheet.Cell(4, genderColumn).GetString());
		Assert.Equal("zongsoft@qq.com", worksheet.Cell(4, emailColumn).GetString());
		Assert.True(worksheet.Cell(4, birthdayColumn).IsEmpty());
		Assert.Equal(new DateTime(1983, 1, 23), worksheet.Cell(5, birthdayColumn).GetDateTime());
		Assert.Equal("yyyy-MM-dd", worksheet.Cell(5, birthdayColumn).Style.DateFormat.Format);
	}

	[Fact]
	public async Task GenerateAsync_SelectedFieldsAndSingleRecord_UsesRequestedProjection()
	{
		using var output = new MemoryStream();
		var user = new User(501, "Ada", "Countess", Gender.Female, new DateTime(1815, 12, 10));
		var options = new DataArchiveGeneratorOptions(nameof(User.Name), nameof(User.Birthday));

		await _generator.GenerateAsync(output, Templates.User.Descriptor, user, options);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var worksheet = Assert.Single(workbook.Worksheets);
		var dataRange = Assert.Single(worksheet.DefinedName(Templates.User.Descriptor.Name).Ranges);

		Assert.Equal(1, dataRange.RowCount());
		Assert.Equal(2, dataRange.ColumnCount());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Name)].Label, worksheet.Cell(3, 1).GetString());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Birthday)].Label, worksheet.Cell(3, 2).GetString());
		Assert.Equal("Ada", worksheet.Cell(4, 1).GetString());
		Assert.Equal(new DateTime(1815, 12, 10), worksheet.Cell(4, 2).GetDateTime());
		Assert.False(worksheet.DefinedNames.TryGetValue(nameof(User.UserId), out _));
	}

	[Fact]
	public async Task GenerateAsync_NullOutput_ThrowsArgumentNullException()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await _generator.GenerateAsync(null, Templates.User.Descriptor, Templates.User.Data));
	}

	[Fact]
	public async Task GenerateAsync_NullModel_ThrowsArgumentNullException()
	{
		using var output = new MemoryStream();
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await _generator.GenerateAsync(output, null, Templates.User.Data));
	}

	private static int GetColumnNumber(IXLWorksheet worksheet, string name) => Assert.Single(worksheet.DefinedName(name).Ranges).FirstColumn().ColumnNumber();
}
