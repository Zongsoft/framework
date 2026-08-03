using Zongsoft.Externals.ClosedXml.Tests.Models;

namespace Zongsoft.Externals.ClosedXml.Tests;

[Collection(CultureSensitiveCollection.Name)]
public class SpreadsheetGeneratorTest
{
	private readonly SpreadsheetGenerator _generator = new();

	[Fact]
	public async Task GenerateAsync_Records_CreatesNamedTableWithHeadersAndTypedData()
	{
		using var output = new MemoryStream();

		await _generator.GenerateAsync(output, Templates.User.Descriptor, Templates.User.Data);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var worksheet = Assert.Single(workbook.Worksheets);
		var table = GetTable(workbook, Templates.User.Descriptor.Name);

		Assert.Equal(Templates.User.Descriptor.Name, table.Name);
		Assert.Equal(3, table.RangeAddress.FirstAddress.RowNumber);
		Assert.Equal(3 + Templates.User.Data.Length, table.RangeAddress.LastAddress.RowNumber);
		Assert.Equal(Templates.User.Data.Length, table.DataRange.RowCount());
		Assert.Equal(Templates.User.Descriptor.Properties.Count, table.ColumnCount());
		Assert.True(table.ShowHeaderRow);
		Assert.False(table.ShowTotalsRow);
		Assert.False(worksheet.DefinedNames.TryGetValue(Templates.User.Descriptor.Name, out _));
		Assert.Equal(XLPageOrientation.Landscape, worksheet.PageSetup.PageOrientation);
		Assert.True(worksheet.Range(1, 1, 1, table.ColumnCount()).IsMerged());
		Assert.True(worksheet.Range(2, 1, 2, table.ColumnCount()).IsMerged());

		var userIdColumn = GetColumnNumber(table, Templates.User.Descriptor, nameof(User.UserId));
		var nameColumn = GetColumnNumber(table, Templates.User.Descriptor, nameof(User.Name));
		var genderColumn = GetColumnNumber(table, Templates.User.Descriptor, nameof(User.Gender));
		var birthdayColumn = GetColumnNumber(table, Templates.User.Descriptor, nameof(User.Birthday));
		var emailColumn = GetColumnNumber(table, Templates.User.Descriptor, nameof(User.Email));

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
	public async Task GenerateAsync_SelectedFieldsAndSingleRecord_CreatesProjectedTable()
	{
		using var output = new MemoryStream();
		var user = new User(501, "Ada", "Countess", Gender.Female, new DateTime(1815, 12, 10));
		var options = new DataArchiveGeneratorOptions(nameof(User.Name), nameof(User.Birthday));

		await _generator.GenerateAsync(output, Templates.User.Descriptor, user, options);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, Templates.User.Descriptor.Name);
		var worksheet = table.Worksheet;

		Assert.Equal(3, table.RangeAddress.FirstAddress.RowNumber);
		Assert.Equal(4, table.RangeAddress.LastAddress.RowNumber);
		Assert.Equal(1, table.DataRange.RowCount());
		Assert.Equal(2, table.ColumnCount());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Name)].Label, worksheet.Cell(3, 1).GetString());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Birthday)].Label, worksheet.Cell(3, 2).GetString());
		Assert.Equal("Ada", worksheet.Cell(4, 1).GetString());
		Assert.Equal(new DateTime(1815, 12, 10), worksheet.Cell(4, 2).GetDateTime());
		Assert.False(worksheet.DefinedNames.TryGetValue(nameof(User.UserId), out _));
	}

	[Theory]
	[InlineData("en-US", "The 'A1' model name cannot be used as an Excel table name.")]
	[InlineData("zh-Hans", "模型名称“A1”不能用作 Excel 数据表名称。")]
	public async Task GenerateAsync_InvalidTableName_ThrowsLocalizedOperationException(string cultureName, string message)
	{
		using var culture = new CultureScope(cultureName);
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(User))
		{
			Name = "A1",
			Title = "Users",
		};

		var exception = await Assert.ThrowsAsync<OperationException>(async () =>
			await _generator.GenerateAsync(output, model, Templates.User.Data));

		Assert.Equal(nameof(OperationException.Argument), exception.Reason);
		Assert.Equal(message, exception.Message);
		Assert.IsType<ArgumentException>(exception.InnerException);
		Assert.Equal(0, output.Length);
	}

	[Fact]
	public async Task GenerateAsync_NullOutput_ThrowsArgumentNullException()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _generator.GenerateAsync(null, Templates.User.Descriptor, Templates.User.Data));
	}

	[Fact]
	public async Task GenerateAsync_NullModel_ThrowsArgumentNullException()
	{
		using var output = new MemoryStream();

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await _generator.GenerateAsync(output, null, Templates.User.Data));
	}

	private static int GetColumnNumber(IXLTable table, ModelDescriptor model, string name)
	{
		var label = model.Properties[name].Label;
		return Assert.Single(table.HeadersRow().Cells(cell => cell.GetString() == label)).Address.ColumnNumber;
	}

	private static IXLTable GetTable(XLWorkbook workbook, string name) =>
		Assert.Single(workbook.Worksheets.SelectMany(worksheet => worksheet.Tables), table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));
}
