namespace Zongsoft.Externals.ClosedXml.Tests;

public class UtilityTest
{
	[Fact]
	public void SetAndGetCellValue_SupportedScalars_PreservesSemanticValues()
	{
		using var workbook = new XLWorkbook();
		var worksheet = workbook.AddWorksheet("Values");
		var date = new DateTime(2024, 2, 29, 13, 45, 12);

		Utility.SetCellValue(worksheet.Cell("A1"), "alpha");
		Utility.SetCellValue(worksheet.Cell("A2"), 42);
		Utility.SetCellValue(worksheet.Cell("A3"), 12.5m);
		Utility.SetCellValue(worksheet.Cell("A4"), true);
		Utility.SetCellValue(worksheet.Cell("A5"), date);

		Assert.Equal("alpha", Utility.GetCellValue(worksheet.Cell("A1")));
		Assert.Equal(42d, Utility.GetCellValue(worksheet.Cell("A2")));
		Assert.Equal(12.5d, Utility.GetCellValue(worksheet.Cell("A3")));
		Assert.Equal(true, Utility.GetCellValue(worksheet.Cell("A4")));
		Assert.Equal(date, Utility.GetCellValue(worksheet.Cell("A5")));
	}

	[Fact]
	public void SetCellValue_NullAndDbNull_WritesBlankCells()
	{
		using var workbook = new XLWorkbook();
		var worksheet = workbook.AddWorksheet("Values");

		Utility.SetCellValue(worksheet.Cell("A1"), null);
		Utility.SetCellValue(worksheet.Cell("A2"), DBNull.Value);

		Assert.True(worksheet.Cell("A1").IsEmpty());
		Assert.True(worksheet.Cell("A2").IsEmpty());
		Assert.Null(Utility.GetCellValue(worksheet.Cell("A1")));
		Assert.Null(Utility.GetCellValue(worksheet.Cell("A2")));
	}

	[Fact]
	public void GetCellValue_Formula_ReturnsFormattedResult()
	{
		using var workbook = new XLWorkbook();
		var worksheet = workbook.AddWorksheet("Formula");
		worksheet.Cell("A1").SetValue(6);
		worksheet.Cell("A2").SetValue(7);
		worksheet.Cell("A3").FormulaA1 = "A1*A2";

		Assert.Equal("42", Utility.GetCellValue(worksheet.Cell("A3")));
	}
}
