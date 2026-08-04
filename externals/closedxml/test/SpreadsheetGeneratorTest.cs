using System.Data;

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
		var worksheet = Assert.Single(workbook.Worksheets, worksheet => worksheet.Visibility == XLWorksheetVisibility.Visible);
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
	public async Task GenerateAsync_SelectedFieldsWithHistoricalAndModernDates_PreservesSemanticTypes()
	{
		using var output = new MemoryStream();
		User[] users =
		[
			new(501, "Ada", "Countess", Gender.Female, new DateTime(1815, 12, 10)),
			new(502, "Eve", "Last Historical Date", Gender.Female, new DateTime(1899, 12, 31)),
			new(503, "Dawn", "First Excel Date", Gender.Female, new DateTime(1900, 1, 1)),
			new(504, "Grape", "Grape Liu", Gender.Female, new DateTime(1983, 1, 23)),
		];
		var options = new DataArchiveGeneratorOptions(nameof(User.Name), nameof(User.Birthday));

		await _generator.GenerateAsync(output, Templates.User.Descriptor, users, options);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, Templates.User.Descriptor.Name);
		var worksheet = table.Worksheet;
		var historicalBirthday = worksheet.Cell(4, 2);
		var lastHistoricalBirthday = worksheet.Cell(5, 2);
		var firstExcelBirthday = worksheet.Cell(6, 2);
		var modernBirthday = worksheet.Cell(7, 2);

		Assert.Equal(3, table.RangeAddress.FirstAddress.RowNumber);
		Assert.Equal(7, table.RangeAddress.LastAddress.RowNumber);
		Assert.Equal(4, table.DataRange.RowCount());
		Assert.Equal(2, table.ColumnCount());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Name)].Label, worksheet.Cell(3, 1).GetString());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Birthday)].Label, worksheet.Cell(3, 2).GetString());
		Assert.Equal("Ada", worksheet.Cell(4, 1).GetString());
		Assert.Equal(XLDataType.Text, historicalBirthday.DataType);
		Assert.Equal("1815-12-10", historicalBirthday.GetString());
		Assert.Equal("1815-12-10", historicalBirthday.GetFormattedString());
		Assert.Equal(XLDataType.Text, lastHistoricalBirthday.DataType);
		Assert.Equal("1899-12-31", lastHistoricalBirthday.GetString());
		Assert.Equal(XLDataType.DateTime, firstExcelBirthday.DataType);
		Assert.Equal(new DateTime(1900, 1, 1), firstExcelBirthday.GetDateTime());
		Assert.Equal("Grape", worksheet.Cell(7, 1).GetString());
		Assert.Equal(XLDataType.DateTime, modernBirthday.DataType);
		Assert.Equal(new DateTime(1983, 1, 23), modernBirthday.GetDateTime());
		Assert.Equal("yyyy-MM-dd", modernBirthday.Style.DateFormat.Format);
		Assert.False(worksheet.DefinedNames.TryGetValue(nameof(User.UserId), out _));
	}

	[Fact]
	public async Task GenerateAsync_EnumColumns_CreatesNullabilityAwareDropdownValidations()
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(EnumValidationRecord)) { Title = "Enum Validations" };
		var record = new EnumValidationRecord
		{
			RequiredGender = Gender.Male,
		};

		await _generator.GenerateAsync(output, model, record);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var requiredCell = table.Worksheet.Cell(4, GetColumnNumber(table, model, nameof(EnumValidationRecord.RequiredGender)));
		var optionalCell = table.Worksheet.Cell(4, GetColumnNumber(table, model, nameof(EnumValidationRecord.OptionalGender)));
		var requiredItems = GetValidationItems(workbook, requiredCell, false, false);
		var optionalItems = GetValidationItems(workbook, optionalCell, true, true);
		var enumNames = Enum.GetNames<Gender>();

		Assert.Equal(nameof(Gender.Male), requiredCell.GetString());
		Assert.True(optionalCell.IsEmpty());
		Assert.Equal(table.ColumnCount(), table.Worksheet.LastColumnUsed().ColumnNumber());
		Assert.Equal(enumNames, requiredItems.Select(item => item.GetText()));
		Assert.DoesNotContain(requiredItems, item => item.IsBlank);
		Assert.Equal(enumNames, optionalItems.Where(item => !item.IsBlank).Select(item => item.GetText()));
		Assert.Single(optionalItems, item => item.IsBlank);
		Assert.All(optionalItems.Where(item => !item.IsBlank), item => Assert.True(item.IsText));
	}

	[Fact]
	public async Task GenerateAsync_BooleanColumns_CreatesNullabilityAwareDropdownValidations()
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(BooleanRecord)) { Title = "Boolean Validations" };
		BooleanRecord[] records =
		[
			new() { RecordId = 1, RequiredValue = true, OptionalValue = null },
			new() { RecordId = 2, RequiredValue = false, OptionalValue = true },
		];

		await _generator.GenerateAsync(output, model, records);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var requiredColumn = GetColumnNumber(table, model, nameof(BooleanRecord.RequiredValue));
		var optionalColumn = GetColumnNumber(table, model, nameof(BooleanRecord.OptionalValue));
		var requiredCells = table.DataRange.Column(requiredColumn).Cells().ToArray();
		var optionalCells = table.DataRange.Column(optionalColumn).Cells().ToArray();
		var requiredItems = GetValidationItems(workbook, requiredCells[0], false, true);
		var optionalItems = GetValidationItems(workbook, optionalCells[0], true, true);

		Assert.Equal(2, requiredCells.Length);
		Assert.Equal(2, optionalCells.Length);
		Assert.Equal(table.ColumnCount(), table.Worksheet.LastColumnUsed().ColumnNumber());
		Assert.All(requiredCells, cell => Assert.True(cell.HasDataValidation));
		Assert.All(optionalCells, cell => Assert.True(cell.HasDataValidation));
		Assert.Equal(XLDataType.Boolean, requiredCells[0].DataType);
		Assert.True(requiredCells[0].GetBoolean());
		Assert.False(requiredCells[1].GetBoolean());
		Assert.True(optionalCells[0].IsEmpty());
		Assert.True(optionalCells[1].GetBoolean());
		Assert.Collection(requiredItems,
			item => { Assert.True(item.IsBoolean); Assert.True(item.GetBoolean()); },
			item => { Assert.True(item.IsBoolean); Assert.False(item.GetBoolean()); });
		Assert.Collection(optionalItems,
			item => { Assert.True(item.IsBoolean); Assert.True(item.GetBoolean()); },
			item => { Assert.True(item.IsBoolean); Assert.False(item.GetBoolean()); },
			item => Assert.True(item.IsBlank));
	}

	[Fact]
	public async Task GenerateAsync_EmptyTitle_UsesModelNameAsDocumentTitle()
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(User))
		{
			Name = "UntitledUsers",
			Title = string.Empty,
		};

		await _generator.GenerateAsync(output, model, Templates.User.Data[0]);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var worksheet = Assert.Single(workbook.Worksheets, worksheet => worksheet.Visibility == XLWorksheetVisibility.Visible);
		var title = worksheet.Cell(1, 1).GetString();

		Assert.Equal(model.Name, worksheet.Name);
		Assert.Equal(model.Name, title);
		Assert.False(string.IsNullOrWhiteSpace(title));
	}

	[Fact]
	public async Task GenerateAsync_SimplexMetadata_SetsPersistedDataTypeRoleAndLengthWidths()
	{
		using var output = new MemoryStream();
		var model = CreateColumnStyleModel();
		var records = CreateColumnStyleRecords();

		await _generator.GenerateAsync(output, model, records);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var shortWidth = GetColumnWidth(table, model, nameof(ColumnStyleRecord.ShortText));
		var mediumWidth = GetColumnWidth(table, model, nameof(ColumnStyleRecord.MediumText));
		var oversizedWidth = GetColumnWidth(table, model, nameof(ColumnStyleRecord.OversizedText));
		var integerWidth = GetColumnWidth(table, model, nameof(ColumnStyleRecord.Quantity));
		var descriptionWidth = GetColumnWidth(table, model, nameof(ColumnStyleRecord.DescriptionText));

		Assert.InRange(shortWidth, 9.99, 10.01);
		Assert.InRange(mediumWidth, 49.99, 50.01);
		Assert.InRange(oversizedWidth, 49.99, 50.01);
		Assert.InRange(integerWidth, 11.99, 12.01);
		Assert.InRange(descriptionWidth, 49.99, 50.01);
		Assert.True(shortWidth < mediumWidth);
		Assert.Equal(mediumWidth, oversizedWidth, 2);
		Assert.All(new[] { shortWidth, mediumWidth, oversizedWidth, integerWidth, descriptionWidth },
			width => Assert.InRange(width, 8, 50));
	}

	[Fact]
	public async Task GenerateAsync_CurrencyRole_UsesPersistedCurrencyNumberFormat()
	{
		using var output = new MemoryStream();
		var model = CreateColumnStyleModel();

		await _generator.GenerateAsync(output, model, CreateColumnStyleRecords());

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var balanceColumn = GetColumnNumber(table, model, nameof(ColumnStyleRecord.Balance));
		var balanceCells = table.DataRange.Column(balanceColumn).Cells().ToArray();

		Assert.Equal(2, balanceCells.Length);
		Assert.Equal(128.5, balanceCells[0].GetDouble());
		Assert.Equal(-12.25, balanceCells[1].GetDouble());
		Assert.All(balanceCells, cell =>
		{
			Assert.Equal(XLDataType.Number, cell.DataType);
			Assert.Equal(7, cell.Style.NumberFormat.NumberFormatId);
		});
	}

	[Fact]
	public async Task GenerateAsync_PrimaryKey_StylesEveryPersistedDataCell()
	{
		using var output = new MemoryStream();
		var model = CreateColumnStyleModel();

		await _generator.GenerateAsync(output, model, CreateColumnStyleRecords());

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var keyColumn = GetColumnNumber(table, model, nameof(ColumnStyleRecord.RecordId));
		var keyCells = table.DataRange.Column(keyColumn).Cells().ToArray();

		Assert.Equal(2, keyCells.Length);
		Assert.All(keyCells, cell =>
		{
			Assert.Equal(XLAlignmentHorizontalValues.Center, cell.Style.Alignment.Horizontal);
			Assert.True(cell.Style.Font.Bold);
			Assert.Equal(XLColor.Maroon, cell.Style.Font.FontColor);
		});
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
		if(string.IsNullOrEmpty(label))
			label = name;

		return Assert.Single(table.HeadersRow().Cells(cell => cell.GetString() == label)).Address.ColumnNumber;
	}

	private static double GetColumnWidth(IXLTable table, ModelDescriptor model, string name) =>
		table.Worksheet.Column(GetColumnNumber(table, model, name)).Width;

	private static ModelDescriptor CreateColumnStyleModel()
	{
		var model = new ModelDescriptor(typeof(ColumnStyleRecord)) { Title = "Column Styles" };
		model.Properties[nameof(ColumnStyleRecord.RecordId)].Label = "K";
		model.Properties[nameof(ColumnStyleRecord.ShortText)].Label = "S";
		model.Properties[nameof(ColumnStyleRecord.MediumText)].Label = "M";
		model.Properties[nameof(ColumnStyleRecord.OversizedText)].Label = "L";
		model.Properties[nameof(ColumnStyleRecord.Quantity)].Label = "I";
		model.Properties[nameof(ColumnStyleRecord.DescriptionText)].Label = "D";
		return model;
	}

	private static ColumnStyleRecord[] CreateColumnStyleRecords() =>
	[
		new() { RecordId = 101, ShortText = "x", MediumText = "x", OversizedText = "x", Quantity = 1, DescriptionText = "x", Balance = 128.5m },
		new() { RecordId = 102, ShortText = "y", MediumText = "y", OversizedText = "y", Quantity = 2, DescriptionText = "y", Balance = -12.25m },
	];

	private static XLCellValue[] GetValidationItems(XLWorkbook workbook, IXLCell cell, bool ignoreBlanks, bool rangeSource)
	{
		Assert.True(cell.HasDataValidation);
		var validation = cell.GetDataValidation();

		Assert.Equal(XLAllowedValues.List, validation.AllowedValues);
		Assert.True(validation.InCellDropdown);
		Assert.Equal(ignoreBlanks, validation.IgnoreBlanks);

		var value = validation.Value;
		var inlineSource = value.Length >= 2 && value[0] == '"' && value[^1] == '"';
		Assert.Equal(!rangeSource, inlineSource);

		if(inlineSource)
			return value[1..^1].Split(',', StringSplitOptions.None).Select(item => (XLCellValue)item).ToArray();

		Assert.Contains('!', value);
		Assert.Empty(workbook.DefinedNames);
		var source = workbook.Range(value);
		Assert.NotNull(source);
		Assert.NotEqual(cell.Worksheet.Name, source.Worksheet.Name);
		Assert.Equal(XLWorksheetVisibility.VeryHidden, source.Worksheet.Visibility);

		return source.Cells()
			.Select(cell => cell.Value)
			.ToArray();
	}

	private static IXLTable GetTable(XLWorkbook workbook, string name) =>
		Assert.Single(workbook.Worksheets.SelectMany(worksheet => worksheet.Tables), table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));

	private sealed class EnumValidationRecord
	{
		public Gender RequiredGender { get; set; }
		public Gender? OptionalGender { get; set; }
	}

	private sealed class ColumnStyleRecord
	{
		[ModelProperty(DbType.Int32, false, IsPrimaryKey = true)]
		public int RecordId { get; set; }

		[ModelProperty(DbType.AnsiString, 8, false)]
		public string ShortText { get; set; }

		[ModelProperty(DbType.AnsiString, 64, false)]
		public string MediumText { get; set; }

		[ModelProperty(DbType.AnsiString, 4096, false)]
		public string OversizedText { get; set; }

		[ModelProperty(DbType.Int32, false)]
		public int Quantity { get; set; }

		[ModelProperty(DbType.AnsiString, 8, false, Role = nameof(ModelPropertyRole.Description))]
		public string DescriptionText { get; set; }

		[ModelProperty(DbType.Decimal, false, Role = nameof(ModelPropertyRole.Currency))]
		public decimal Balance { get; set; }
	}
}
