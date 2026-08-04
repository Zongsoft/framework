using Zongsoft.Externals.ClosedXml.Tests.Models;

namespace Zongsoft.Externals.ClosedXml.Tests;

[Collection(CultureSensitiveCollection.Name)]
public class SpreadsheetExtractorTest
{
	private readonly SpreadsheetExtractor _extractor = new();
	private readonly SpreadsheetGenerator _generator = new();

	[Fact]
	public async Task ExtractAsync_GeneratedTable_RestoresTypedRecords()
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
	public async Task ExtractAsync_GeneratedBooleanTable_RestoresTrueFalseAndNull()
	{
		using var stream = new MemoryStream();
		var model = new ModelDescriptor(typeof(BooleanRecord)) { Title = "Boolean Records" };
		BooleanRecord[] records =
		[
			new() { RecordId = 1, RequiredValue = true, OptionalValue = null },
			new() { RecordId = 2, RequiredValue = false, OptionalValue = true },
			new() { RecordId = 3, RequiredValue = true, OptionalValue = false },
		];
		await _generator.GenerateAsync(stream, model, records);
		stream.Position = 0;

		var result = _extractor.ExtractAsync<BooleanRecord>(stream, new DataArchiveExtractorOptions(model))
			.Synchronize()
			.ToArray();

		Assert.Collection(result,
			first =>
			{
				Assert.Equal(1, first.RecordId);
				Assert.True(first.RequiredValue);
				Assert.Null(first.OptionalValue);
			},
			second =>
			{
				Assert.Equal(2, second.RecordId);
				Assert.False(second.RequiredValue);
				Assert.True(second.OptionalValue);
			},
			third =>
			{
				Assert.Equal(3, third.RecordId);
				Assert.True(third.RequiredValue);
				Assert.False(third.OptionalValue);
			});
	}

	[Fact]
	public void ExtractAsync_BooleanTextTable_ConvertsTrueFalseAndNull()
	{
		var model = new ModelDescriptor(typeof(BooleanRecord));
		using var stream = CreateWorkbook(workbook =>
		{
			var worksheet = workbook.AddWorksheet("Booleans");
			worksheet.Cell("A1").SetValue(nameof(BooleanRecord.RecordId));
			worksheet.Cell("B1").SetValue(nameof(BooleanRecord.RequiredValue));
			worksheet.Cell("C1").SetValue(nameof(BooleanRecord.OptionalValue));
			worksheet.Cell("A2").SetValue(1);
			worksheet.Cell("B2").SetValue("TRUE");
			worksheet.Cell("A3").SetValue(2);
			worksheet.Cell("B3").SetValue("FALSE");
			worksheet.Cell("C3").SetValue("TRUE");
			worksheet.Cell("A4").SetValue(3);
			worksheet.Cell("B4").SetValue("TRUE");
			worksheet.Cell("C4").SetValue("FALSE");
			worksheet.Range("A1:C4").CreateTable(model.Name);
		});

		var result = _extractor.ExtractAsync<BooleanRecord>(stream, new DataArchiveExtractorOptions(model))
			.Synchronize()
			.ToArray();

		Assert.Collection(result,
			first =>
			{
				Assert.Equal(1, first.RecordId);
				Assert.True(first.RequiredValue);
				Assert.Null(first.OptionalValue);
			},
			second =>
			{
				Assert.Equal(2, second.RecordId);
				Assert.False(second.RequiredValue);
				Assert.True(second.OptionalValue);
			},
			third =>
			{
				Assert.Equal(3, third.RecordId);
				Assert.True(third.RequiredValue);
				Assert.False(third.OptionalValue);
			});
	}

	[Fact]
	public void ExtractAsync_ModelNamedTableWithReorderedColumnsAndTrailingRows_MapsFieldsAndSkipsEmptyRows()
	{
		using var stream = CreateImportTableWorkbook();

		var result = _extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor))
			.Synchronize()
			.ToArray();

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

	[Theory]
	[InlineData("en-US", "The 'User' Excel table was not found.")]
	[InlineData("zh-Hans", "找不到 Excel 数据表“User”。")]
	public void ExtractAsync_MissingTable_ThrowsLocalizedOperationException(string cultureName, string message)
	{
		using var culture = new CultureScope(cultureName);
		using var stream = CreateWorkbook(workbook => workbook.AddWorksheet("Users"));

		var exception = Assert.Throws<OperationException>(() =>
			_extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor)));

		Assert.Equal(nameof(OperationException.Unprocessed), exception.Reason);
		Assert.Equal(message, exception.Message);
	}

	[Fact]
	public void ExtractAsync_LegacyDefinedNameOnly_ThrowsTableNotFound()
	{
		using var culture = new CultureScope("en-US");
		using var stream = CreateLegacyDefinedNameWorkbook();

		var exception = Assert.Throws<OperationException>(() =>
			_extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor)));

		Assert.Equal(nameof(OperationException.Unprocessed), exception.Reason);
		Assert.Equal("The 'User' Excel table was not found.", exception.Message);
	}

	[Theory]
	[InlineData("en-US", "The 'User' Excel table does not define any model fields.")]
	[InlineData("zh-Hans", "Excel 数据表“User”没有定义任何模型字段。")]
	public void ExtractAsync_UnrecognizedTableFields_ThrowsLocalizedOperationException(string cultureName, string message)
	{
		using var culture = new CultureScope(cultureName);
		using var stream = CreateWorkbook(workbook =>
		{
			var worksheet = workbook.AddWorksheet("Users");
			worksheet.Cell("A1").SetValue("ExternalId");
			worksheet.Cell("B1").SetValue("DisplayName");
			worksheet.Cell("A2").SetValue(901);
			worksheet.Cell("B2").SetValue("Unknown");
			worksheet.Range("A1:B2").CreateTable(Templates.User.Descriptor.Name);
		});

		var exception = Assert.Throws<OperationException>(() =>
			_extractor.ExtractAsync<User>(stream, new DataArchiveExtractorOptions(Templates.User.Descriptor)));

		Assert.Equal(nameof(OperationException.Unprocessed), exception.Reason);
		Assert.Equal(message, exception.Message);
	}

	[Fact]
	public void ExtractAsync_MissingSourceWorksheet_ThrowsLocalizedOperationException()
	{
		using var culture = new CultureScope("en-US");
		using var stream = CreateWorkbook(workbook =>
		{
			var worksheet = workbook.AddWorksheet("Data");
			worksheet.Cell("A1").SetValue(nameof(User.UserId));
			worksheet.Cell("A2").SetValue(801);
			worksheet.Range("A1:A2").CreateTable(Templates.User.Descriptor.Name);
		});
		var options = new DataArchiveExtractorOptions(Templates.User.Descriptor) { Source = "Missing" };

		var exception = Assert.Throws<OperationException>(() => _extractor.ExtractAsync<User>(stream, options));

		Assert.Equal(nameof(OperationException.Unprocessed), exception.Reason);
		Assert.Equal("The 'Missing' Excel worksheet was not found.", exception.Message);
	}

	[Fact]
	public void ExtractAsync_NullInput_ThrowsArgumentNullException()
	{
		Assert.Throws<ArgumentNullException>(() =>
			_extractor.ExtractAsync<User>(null, new DataArchiveExtractorOptions(Templates.User.Descriptor)));
	}

	[Fact]
	public void ExtractAsync_NullOptions_ThrowsArgumentNullException()
	{
		using var stream = CreateImportTableWorkbook();

		Assert.Throws<ArgumentNullException>(() => _extractor.ExtractAsync<User>(stream, null));
	}

	private static MemoryStream CreateImportTableWorkbook()
	{
		return CreateWorkbook(workbook =>
		{
			var ignored = workbook.AddWorksheet("Ignored");
			ignored.Cell("A1").SetValue("OtherId");
			ignored.Cell("A2").SetValue(1);
			ignored.Range("A1:A2").CreateTable("Other");

			var worksheet = workbook.AddWorksheet("Import");
			worksheet.Cell("B2").SetValue(nameof(User.Birthday));
			worksheet.Cell("C2").SetValue(nameof(User.Name));
			worksheet.Cell("D2").SetValue(nameof(User.UserId));
			worksheet.Cell("B3").SetValue(new DateTime(1906, 12, 9));
			worksheet.Cell("C3").SetValue("Grace");
			worksheet.Cell("D3").SetValue(701);
			worksheet.Range("B2:D3").CreateTable(Templates.User.Descriptor.Name);

			// Row 4 is deliberately empty and row 5 lies outside the table's saved range.
			worksheet.Cell("C5").SetValue("Linus");
			worksheet.Cell("D5").SetValue(702);
		});
	}

	private static MemoryStream CreateLegacyDefinedNameWorkbook()
	{
		return CreateWorkbook(workbook =>
		{
			var worksheet = workbook.AddWorksheet("Users");
			worksheet.Cell("A1").SetValue(nameof(User.UserId)).AddToNamed(nameof(User.UserId), XLScope.Worksheet);
			worksheet.Cell("B1").SetValue(nameof(User.Name)).AddToNamed(nameof(User.Name), XLScope.Worksheet);
			worksheet.Cell("A2").SetValue(601);
			worksheet.Cell("B2").SetValue("Legacy");
			worksheet.Range("A2:B2").AddToNamed(Templates.User.Descriptor.Name, XLScope.Workbook);
		});
	}

	private static MemoryStream CreateWorkbook(Action<XLWorkbook> configure)
	{
		using var workbook = new XLWorkbook();
		configure(workbook);

		var stream = new MemoryStream();
		workbook.SaveAs(stream);
		stream.Position = 0;
		return stream;
	}
}
