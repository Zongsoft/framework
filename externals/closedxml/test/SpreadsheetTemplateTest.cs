namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetTemplateTest : IDisposable
{
	private readonly string _directory;

	public SpreadsheetTemplateTest()
	{
		_directory = Path.Combine(Path.GetTempPath(), $"zongsoft-closedxml-{Guid.NewGuid():N}");
		Directory.CreateDirectory(_directory);
	}

	[Fact]
	public void Create_ExistingWorkbook_LoadsMetadataAndOpensContent()
	{
		var path = CreateWorkbook("invoice.xlsx", "Invoice Report", "Monthly invoice template");
		var template = SpreadsheetTemplate.Create(path);

		Assert.NotNull(template);
		Assert.Equal("invoice", template.Name);
		Assert.Equal("Invoice Report", template.Title);
		Assert.Equal("Monthly invoice template", template.Description);
		Assert.Same(Spreadsheet.Format, template.Format);
		Assert.Equal(path, template.FilePath);
		Assert.Equal($"invoice@{path}", template.ToString());

		using var stream = template.Open();
		using var workbook = new XLWorkbook(stream);
		Assert.Equal("template-content", workbook.Worksheet("Template").Cell("B2").GetString());
	}

	[Fact]
	public void Create_SamePath_ProducesEqualTemplatesWithEqualHashCodes()
	{
		var path = CreateWorkbook("same.xlsx", null, null);
		var first = SpreadsheetTemplate.Create(path);
		var second = SpreadsheetTemplate.Create(path);

		Assert.Equal(first, second);
		Assert.True(first.Equals((object)second));
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.False(first.Equals(null));
	}

	[Fact]
	public void Create_MissingPath_ReturnsNull()
	{
		Assert.Null(SpreadsheetTemplate.Create(null));
		Assert.Null(SpreadsheetTemplate.Create(string.Empty));
		Assert.Null(SpreadsheetTemplate.Create(Path.Combine(_directory, "missing.xlsx")));
	}

	[Fact]
	public void Provider_NestedWorkbook_FindsCaseInsensitivelyAndFiltersFormat()
	{
		var nested = Directory.CreateDirectory(Path.Combine(_directory, "nested")).FullName;
		var path = CreateWorkbook(Path.Combine("nested", "monthly.xlsx"), "Monthly", "Nested template");
		var provider = new SpreadsheetTemplateProvider(_directory);

		var template = provider.GetTemplate("MONTHLY", Spreadsheet.Format.Name);

		Assert.NotNull(template);
		Assert.Equal(path, Assert.IsType<SpreadsheetTemplate>(template).FilePath);
		Assert.Same(template, provider.GetTemplate("monthly"));
		Assert.Null(provider.GetTemplate("monthly", "PDF"));
		Assert.Null(provider.GetTemplate("missing"));
	}

	public void Dispose()
	{
		if(Directory.Exists(_directory))
			Directory.Delete(_directory, true);
	}

	private string CreateWorkbook(string relativePath, string title, string comments)
	{
		var path = Path.Combine(_directory, relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path));

		using var workbook = new XLWorkbook();
		workbook.Properties.Title = title;
		workbook.Properties.Comments = comments;
		workbook.AddWorksheet("Template").Cell("B2").SetValue("template-content");
		workbook.SaveAs(path);
		return path;
	}
}
