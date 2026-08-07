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
		Assert.Equal(8, table.RangeAddress.LastAddress.RowNumber);
		Assert.Equal(Templates.User.Data.Length, table.DataRange.RowCount());
		Assert.Equal(Templates.User.Descriptor.Properties.Count, table.ColumnCount());
		Assert.True(table.ShowHeaderRow);
		Assert.False(table.ShowTotalsRow);
		Assert.False(worksheet.DefinedNames.TryGetValue(Templates.User.Descriptor.Name, out _));

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
	}

	[Theory]
	[InlineData(true, 0)]
	[InlineData(false, 0)]
	[InlineData(false, 1)]
	[InlineData(false, 9)]
	public async Task GenerateAsync_RecordCount_UsesActualRowsAndEditableRowsForEmptyData(bool nullData, int recordCount)
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(DateValidationRecord)) { Title = "Record Count" };
		var records = System.Linq.Enumerable.Range(0, recordCount)
			.Select(index => new DateValidationRecord { Birthday = new DateTime(2024, 1, 1).AddDays(index) })
			.ToArray();

		await _generator.GenerateAsync(output, model, nullData ? null : records);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var cells = table.DataRange.FirstColumn().Cells().ToArray();
		var rowCount = table.DataRange.RowCount();

		if(recordCount == 0)
			Assert.True(rowCount > 0);
		else
			Assert.Equal(recordCount, rowCount);

		Assert.Equal(3 + rowCount, table.RangeAddress.LastAddress.RowNumber);
		Assert.All(cells, cell => Assert.True(cell.HasDataValidation));

		if(recordCount == 0)
			Assert.All(cells, cell => Assert.True(cell.IsEmpty()));
		else
			Assert.Equal(records.Select(record => record.Birthday.Value), cells.Select(cell => cell.GetDateTime()));
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
		Assert.Equal(users.Length, table.DataRange.RowCount());
		Assert.Equal(2, table.ColumnCount());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Name)].Label, worksheet.Cell(3, 1).GetString());
		Assert.Equal(Templates.User.Descriptor.Properties[nameof(User.Birthday)].Label, worksheet.Cell(3, 2).GetString());
		Assert.Equal("Ada", worksheet.Cell(4, 1).GetString());
		Assert.Equal(XLDataType.Text, historicalBirthday.DataType);
		Assert.Equal("1815-12-10", historicalBirthday.GetString());
		Assert.True(historicalBirthday.HasDataValidation);
		Assert.Equal(XLDataType.Text, lastHistoricalBirthday.DataType);
		Assert.Equal("1899-12-31", lastHistoricalBirthday.GetString());
		Assert.Equal(XLDataType.DateTime, firstExcelBirthday.DataType);
		Assert.Equal(new DateTime(1900, 1, 1), firstExcelBirthday.GetDateTime());
		Assert.Equal("Grape", worksheet.Cell(7, 1).GetString());
		Assert.Equal(XLDataType.DateTime, modernBirthday.DataType);
		Assert.Equal(new DateTime(1983, 1, 23), modernBirthday.GetDateTime());
		Assert.True(modernBirthday.HasDataValidation);
		Assert.False(worksheet.DefinedNames.TryGetValue(nameof(User.UserId), out _));
	}

	[Fact]
	public async Task GenerateAsync_FieldOptions_ProjectFormatAndApplyTextModes()
	{
		using var culture = new CultureScope("en-US");
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(FieldOptionsRecord)) { Title = "Field Options" };
		var record = new FieldOptionsRecord
		{
			Value = 1234.5m,
			WrappedText = "wrapped",
			PlainText = "plain",
			ShrunkText = "shrunk",
		};
		DataArchiveField[] fields =
		[
			new(nameof(FieldOptionsRecord.WrappedText), "Wrapped") { TextMode = DataArchiveFieldTextMode.Wrap },
			new(nameof(FieldOptionsRecord.Value), "Formatted") { Format = "N2" },
			new(nameof(FieldOptionsRecord.ShrunkText), "Shrunk") { TextMode = DataArchiveFieldTextMode.Shrink },
			new(nameof(FieldOptionsRecord.PlainText), "Plain") { TextMode = DataArchiveFieldTextMode.None },
		];
		var options = new DataArchiveGeneratorOptions(fields);

		await _generator.GenerateAsync(output, model, record, options);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var worksheet = table.Worksheet;
		var row = table.DataRange.FirstRow();

		Assert.Equal(new[] { "Wrapped", "Formatted", "Shrunk", "Plain" }, table.HeadersRow().Cells().Select(cell => cell.GetString()));
		Assert.Equal("wrapped", row.Cell(1).GetString());
		Assert.Equal("1,234.50", row.Cell(2).GetString());
		Assert.Equal(XLDataType.Text, row.Cell(2).DataType);
		Assert.NotEqual("N2", row.Cell(2).Style.NumberFormat.Format);
		Assert.Equal("shrunk", row.Cell(3).GetString());
		Assert.Equal("plain", row.Cell(4).GetString());

		AssertTextMode(row.Cell(1), true, false);
		AssertTextMode(row.Cell(3), false, true);
		AssertTextMode(row.Cell(4), false, false);

		var futureRow = table.RangeAddress.LastAddress.RowNumber + 1;
		AssertTextMode(worksheet.Cell(futureRow, 1), true, false);
		AssertTextMode(worksheet.Cell(futureRow, 3), false, true);
		AssertTextMode(worksheet.Cell(futureRow, 4), false, false);

		static void AssertTextMode(IXLCell cell, bool wrap, bool shrink)
		{
			Assert.Equal(wrap, cell.Style.Alignment.WrapText);
			Assert.Equal(shrink, cell.Style.Alignment.ShrinkToFit);
		}
	}

	[Fact]
	public async Task GenerateAsync_DescriptionRole_DefaultWrapAndExplicitNonePersistToCellsAndColumns()
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(DescriptionTextModeRecord)) { Title = "Description Text Modes" };
		var record = new DescriptionTextModeRecord
		{
			DefaultDescription = "wrapped by role",
			PlainDescription = "not wrapped by field",
		};
		DataArchiveField[] fields =
		[
			new(nameof(DescriptionTextModeRecord.DefaultDescription)),
			new(nameof(DescriptionTextModeRecord.PlainDescription)) { TextMode = DataArchiveFieldTextMode.None },
		];

		await _generator.GenerateAsync(output, model, record, new DataArchiveGeneratorOptions(fields));

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var worksheet = table.Worksheet;
		var row = table.DataRange.FirstRow();

		Assert.Equal(record.DefaultDescription, row.Cell(1).GetString());
		Assert.Equal(record.PlainDescription, row.Cell(2).GetString());
		Assert.True(row.Cell(1).Style.Alignment.WrapText);
		Assert.True(worksheet.Column(1).Style.Alignment.WrapText);
		Assert.False(row.Cell(2).Style.Alignment.WrapText);
		Assert.False(worksheet.Column(2).Style.Alignment.WrapText);
	}

	[Theory]
	[InlineData("en-US", "Invalid value", "The value of 'Birthday' must be a valid date between 1900-01-01 and 9999-12-31.")]
	[InlineData("zh-Hans", "输入值无效", "“Birthday”必须是 1900-01-01 到 9999-12-31 之间的有效日期。")]
	public async Task GenerateAsync_DateColumns_CreatesLocalizedNativeValidation(string cultureName, string errorTitle, string errorMessage)
	{
		using var culture = new CultureScope(cultureName);
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(DateValidationRecord)) { Title = "Date Validation" };
		model.Properties[nameof(DateValidationRecord.Birthday)].Label = "Birthday";
		DateValidationRecord[] records =
		[
			new() { Birthday = new DateTime(1900, 1, 1) },
			new() { Birthday = new DateTime(2024, 2, 29) },
		];

		await _generator.GenerateAsync(output, model, records);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var cells = table.DataRange.FirstColumn().Cells().ToArray();

		Assert.Equal(records.Length, cells.Length);
		Assert.All(cells, cell => Assert.True(cell.HasDataValidation));
		var validation = cells[0].GetDataValidation();
		Assert.Equal(XLDataType.DateTime, cells[0].DataType);
		Assert.Equal(new DateTime(1900, 1, 1), cells[0].GetDateTime());
		Assert.Equal(XLDataType.DateTime, cells[1].DataType);
		Assert.Equal(new DateTime(2024, 2, 29), cells[1].GetDateTime());
		Assert.Equal(XLAllowedValues.Date, validation.AllowedValues);
		Assert.Equal(XLOperator.Between, validation.Operator);
		Assert.Equal("1", validation.MinValue);
		Assert.Equal(DateTime.MaxValue.Date.ToOADate().ToString(System.Globalization.CultureInfo.InvariantCulture), validation.MaxValue);
		Assert.Equal(table.DataRange.RangeAddress, Assert.Single(validation.Ranges).RangeAddress);
		Assert.True(validation.IgnoreBlanks);
		Assert.True(validation.ShowErrorMessage);
		Assert.Equal(XLErrorStyle.Stop, validation.ErrorStyle);
		Assert.Equal(errorTitle, validation.ErrorTitle);
		Assert.Equal(errorMessage, validation.ErrorMessage);
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

		Assert.Equal(1, table.DataRange.RowCount());
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

		Assert.Equal(records.Length, requiredCells.Length);
		Assert.Equal(records.Length, optionalCells.Length);
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

	[Theory]
	[InlineData("en-US", "Invalid value", "The value of 'S' is required and cannot exceed 8 characters.", "The value of 'O' cannot exceed 8 characters.", "The value of 'I' must be a whole number between -2147483648 and 2147483647.", "The value of 'B' must be a number.")]
	[InlineData("zh-Hans", "输入值无效", "“S”不能为空且不能超过 8 个字符。", "“O”的内容不能超过 8 个字符。", "“I”必须是 -2147483648 到 2147483647 之间的整数。", "“B”必须是数值。")]
	public async Task GenerateAsync_SimplexDataTypes_CreatesLocalizedInputValidations(
		string cultureName,
		string errorTitle,
		string requiredTextError,
		string optionalTextError,
		string integerError,
		string numberError)
	{
		using var culture = new CultureScope(cultureName);
		using var output = new MemoryStream();
		var model = CreateSimplexValidationModel();

		await _generator.GenerateAsync(output, model, CreateSimplexValidationRecords());

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var shortTextColumn = GetColumnNumber(table, model, nameof(SimplexValidationRecord.ShortText));
		var optionalTextColumn = GetColumnNumber(table, model, nameof(SimplexValidationRecord.OptionalText));
		var quantityColumn = GetColumnNumber(table, model, nameof(SimplexValidationRecord.Quantity));
		var balanceColumn = GetColumnNumber(table, model, nameof(SimplexValidationRecord.Balance));
		var requiredTextValidation = GetValidation(table, shortTextColumn);
		var optionalTextValidation = GetValidation(table, optionalTextColumn);
		var integerValidation = GetValidation(table, quantityColumn);
		var numberValidation = GetValidation(table, balanceColumn);

		Assert.Equal(XLAllowedValues.TextLength, requiredTextValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, requiredTextValidation.Operator);
		Assert.Equal("1", requiredTextValidation.MinValue);
		Assert.Equal("8", requiredTextValidation.MaxValue);
		AssertValidation(requiredTextValidation, table.DataRange.Column(shortTextColumn).RangeAddress, false, errorTitle, requiredTextError);

		Assert.Equal(XLAllowedValues.TextLength, optionalTextValidation.AllowedValues);
		Assert.Equal(XLOperator.EqualOrLessThan, optionalTextValidation.Operator);
		Assert.Equal("8", optionalTextValidation.MinValue);
		AssertValidation(optionalTextValidation, table.DataRange.Column(optionalTextColumn).RangeAddress, true, errorTitle, optionalTextError);

		Assert.Equal(XLAllowedValues.WholeNumber, integerValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, integerValidation.Operator);
		Assert.Equal(int.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), integerValidation.MinValue);
		Assert.Equal(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), integerValidation.MaxValue);
		AssertValidation(integerValidation, table.DataRange.Column(quantityColumn).RangeAddress, false, errorTitle, integerError);

		Assert.Equal(XLAllowedValues.Decimal, numberValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, numberValidation.Operator);
		Assert.True(double.Parse(numberValidation.MinValue, System.Globalization.CultureInfo.InvariantCulture) < 0);
		Assert.True(double.Parse(numberValidation.MaxValue, System.Globalization.CultureInfo.InvariantCulture) > 0);
		AssertValidation(numberValidation, table.DataRange.Column(balanceColumn).RangeAddress, true, errorTitle, numberError);

		static IXLDataValidation GetValidation(IXLTable table, int columnNumber)
		{
			var cell = table.DataRange.FirstRow().Cell(columnNumber);
			Assert.True(cell.HasDataValidation);
			return cell.GetDataValidation();
		}

		static void AssertValidation(
			IXLDataValidation validation,
			IXLRangeAddress expectedRange,
			bool ignoreBlanks,
			string errorTitle,
			string errorMessage)
		{
			Assert.Equal(expectedRange, Assert.Single(validation.Ranges).RangeAddress);
			Assert.Equal(ignoreBlanks, validation.IgnoreBlanks);
			Assert.True(validation.ShowErrorMessage);
			Assert.Equal(XLErrorStyle.Stop, validation.ErrorStyle);
			Assert.Equal(errorTitle, validation.ErrorTitle);
			Assert.Equal(errorMessage, validation.ErrorMessage);
		}
	}

	[Fact]
	public async Task GenerateAsync_ClrNullableDataTypes_UseCombinedNullability()
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(ClrNullableValidationRecord)) { Title = "CLR Nullable Validation" };

		await _generator.GenerateAsync(output, model, new ClrNullableValidationRecord[] { new() });

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);
		var dateValidation = GetValidation(nameof(ClrNullableValidationRecord.Date));
		var integerValidation = GetValidation(nameof(ClrNullableValidationRecord.Integer));
		var numberValidation = GetValidation(nameof(ClrNullableValidationRecord.Number));

		Assert.Equal(XLAllowedValues.Date, dateValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, dateValidation.Operator);
		Assert.Equal("1", dateValidation.MinValue);
		Assert.True(dateValidation.IgnoreBlanks);

		Assert.Equal(XLAllowedValues.WholeNumber, integerValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, integerValidation.Operator);
		Assert.Equal(int.MinValue.ToString(System.Globalization.CultureInfo.InvariantCulture), integerValidation.MinValue);
		Assert.Equal(int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture), integerValidation.MaxValue);
		Assert.True(integerValidation.IgnoreBlanks);

		Assert.Equal(XLAllowedValues.Decimal, numberValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, numberValidation.Operator);
		Assert.True(numberValidation.IgnoreBlanks);

		IXLDataValidation GetValidation(string name)
		{
			var column = GetColumnNumber(table, model, name);
			var cell = table.DataRange.FirstRow().Cell(column);
			Assert.True(cell.HasDataValidation);
			var validation = cell.GetDataValidation();
			Assert.Equal(table.DataRange.Column(column).RangeAddress, Assert.Single(validation.Ranges).RangeAddress);
			return validation;
		}
	}

	[Fact]
	public async Task GenerateAsync_GeneratedOrDefaultedProperties_AllowBlankInput()
	{
		using var output = new MemoryStream();
		var model = new ModelDescriptor(typeof(GeneratedOrDefaultedValidationRecord)) { Title = "Optional Validation" };
		var dbNullProperty = Assert.IsType<ModelPropertyDescriptor.SimplexPropertyDescriptor>(model.Properties[nameof(GeneratedOrDefaultedValidationRecord.DbNullValue)]);
		dbNullProperty.DefaultValue = DBNull.Value;

		await _generator.GenerateAsync(output, model, new GeneratedOrDefaultedValidationRecord[] { new() { Creation = new DateTime(2024, 1, 15) } });

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var table = GetTable(workbook, model.Name);

		AssertIntegerValidation(nameof(GeneratedOrDefaultedValidationRecord.SequenceValue), true);
		AssertIntegerValidation(nameof(GeneratedOrDefaultedValidationRecord.DefaultValue), true);
		AssertIntegerValidation(nameof(GeneratedOrDefaultedValidationRecord.DbNullValue), true);
		AssertIntegerValidation(nameof(GeneratedOrDefaultedValidationRecord.RequiredValue), false);
		AssertIntegerValidation(nameof(GeneratedOrDefaultedValidationRecord.RequiredPrimaryKey), false);

		var creationValidation = GetValidation(nameof(GeneratedOrDefaultedValidationRecord.Creation));
		Assert.Equal(XLAllowedValues.Date, creationValidation.AllowedValues);
		Assert.Equal(XLOperator.Between, creationValidation.Operator);
		Assert.True(creationValidation.IgnoreBlanks);

		void AssertIntegerValidation(string name, bool ignoreBlanks)
		{
			var validation = GetValidation(name);
			Assert.Equal(XLAllowedValues.WholeNumber, validation.AllowedValues);
			Assert.Equal(XLOperator.Between, validation.Operator);
			Assert.Equal(ignoreBlanks, validation.IgnoreBlanks);
		}

		IXLDataValidation GetValidation(string name)
		{
			var column = GetColumnNumber(table, model, name);
			var cell = table.DataRange.FirstRow().Cell(column);
			Assert.True(cell.HasDataValidation);
			var validation = cell.GetDataValidation();
			Assert.Equal(table.DataRange.Column(column).RangeAddress, Assert.Single(validation.Ranges).RangeAddress);
			Assert.True(validation.ShowErrorMessage);
			Assert.Equal(XLErrorStyle.Stop, validation.ErrorStyle);
			return validation;
		}
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

	private static ModelDescriptor CreateSimplexValidationModel()
	{
		var model = new ModelDescriptor(typeof(SimplexValidationRecord)) { Title = "Simplex Validations" };
		model.Properties[nameof(SimplexValidationRecord.ShortText)].Label = "S";
		model.Properties[nameof(SimplexValidationRecord.OptionalText)].Label = "O";
		model.Properties[nameof(SimplexValidationRecord.Quantity)].Label = "I";
		model.Properties[nameof(SimplexValidationRecord.Balance)].Label = "B";
		return model;
	}

	private static SimplexValidationRecord[] CreateSimplexValidationRecords() =>
	[
		new() { ShortText = "x", Quantity = 1, Balance = 128.5m },
		new() { ShortText = "y", OptionalText = "z", Quantity = 2, Balance = -12.25m },
	];

	private static XLCellValue[] GetValidationItems(XLWorkbook workbook, IXLCell cell, bool ignoreBlanks, bool rangeSource)
	{
		Assert.True(cell.HasDataValidation);
		var validation = cell.GetDataValidation();

		Assert.Equal(XLAllowedValues.List, validation.AllowedValues);
		Assert.True(validation.InCellDropdown);
		Assert.Equal(ignoreBlanks, validation.IgnoreBlanks);
		Assert.True(validation.ShowErrorMessage);
		Assert.Equal(XLErrorStyle.Stop, validation.ErrorStyle);
		Assert.False(string.IsNullOrWhiteSpace(validation.ErrorTitle));
		Assert.False(string.IsNullOrWhiteSpace(validation.ErrorMessage));

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

	private sealed class SimplexValidationRecord
	{
		[ModelProperty(DbType.AnsiString, 8, false)]
		public string ShortText { get; set; }

		[ModelProperty(DbType.AnsiString, 8, true)]
		public string OptionalText { get; set; }

		[ModelProperty(DbType.Int32, false)]
		public int Quantity { get; set; }

		[ModelProperty(DbType.Decimal, true, Role = nameof(ModelPropertyRole.Currency))]
		public decimal? Balance { get; set; }
	}

	private sealed class DateValidationRecord
	{
		[ModelProperty(DbType.DateTime, true)]
		public DateTime? Birthday { get; set; }
	}

	private sealed class ClrNullableValidationRecord
	{
		[ModelProperty(DbType.Date, false)]
		public DateOnly? Date { get; set; }

		[ModelProperty(DbType.Int32, false)]
		public int? Integer { get; set; }

		[ModelProperty(DbType.Decimal, false)]
		public decimal? Number { get; set; }
	}

	private sealed class GeneratedOrDefaultedValidationRecord
	{
		[ModelProperty(DbType.Int32, false, Sequence = "#")]
		public int SequenceValue { get; set; }

		[ModelProperty(DbType.Int32, false, 7)]
		public int DefaultValue { get; set; }

		[ModelProperty(DbType.Int32, false)]
		public int DbNullValue { get; set; }

		[ModelProperty(DbType.Int32, false)]
		public int RequiredValue { get; set; }

		[ModelProperty(DbType.Int32, false, IsPrimaryKey = true)]
		public int RequiredPrimaryKey { get; set; }

		[ModelProperty(DbType.DateTime, false, DefaultValue = "now()")]
		public DateTime Creation { get; set; }
	}

	private sealed class FieldOptionsRecord
	{
		public decimal Value { get; set; }
		public string WrappedText { get; set; }
		public string PlainText { get; set; }
		public string ShrunkText { get; set; }
	}

	private sealed class DescriptionTextModeRecord
	{
		[ModelProperty(DbType.String, true, Role = nameof(ModelPropertyRole.Description))]
		public string DefaultDescription { get; set; }

		[ModelProperty(DbType.String, true, Role = nameof(ModelPropertyRole.Description))]
		public string PlainDescription { get; set; }
	}
}
