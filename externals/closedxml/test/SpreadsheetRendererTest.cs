namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetRendererTest
{
	private readonly SpreadsheetRenderer _renderer = new();

	[Fact]
	public async Task RenderAsync_DataAndParameters_RendersExpectedWorkbookValues()
	{
		using var output = new MemoryStream();
		var data = new { Templates.ApartmentUsage.Usages };
		var parameters = new[]
		{
			new KeyValuePair<string, object>(nameof(Templates.ApartmentUsage.Park), Templates.ApartmentUsage.Park),
		};

		await _renderer.RenderAsync(output, Templates.ApartmentUsage.Template, data, parameters);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var worksheet = Assert.Single(workbook.Worksheets);
		var usages = Templates.ApartmentUsage.Usages;

		Assert.Equal(Templates.ApartmentUsage.Park.Name, worksheet.Cell("A1").GetString());
		Assert.Equal(usages[0].Apartment.BuildingId, worksheet.Cell("A4").GetValue<int>());
		Assert.Equal(usages[0].Apartment.Building.Name, worksheet.Cell("B4").GetString());
		Assert.Equal(usages[0].ApartmentId, worksheet.Cell("C4").GetValue<int>());
		Assert.Equal(usages[0].Asset.AssetNo, worksheet.Cell("F4").GetString());
		Assert.Equal(usages[0].Asset.Item.Name, worksheet.Cell("G4").GetString());
		Assert.Equal(usages[0].Quantity, worksheet.Cell("I4").GetDouble());
		Assert.True(worksheet.Cell("J4").IsEmpty());
		Assert.Equal(usages[^1].ApartmentId, worksheet.Cell("C7").GetValue<int>());
		Assert.Equal(usages[^1].AssetId, worksheet.Cell("E7").GetValue<long>());
		var previousDate = DateTime.Parse(worksheet.Cell("H7").GetString(), CultureInfo.CurrentCulture);
		var collectionDate = double.Parse(worksheet.Cell("L7").GetString(), CultureInfo.InvariantCulture);
		Assert.Equal(usages[^1].Date.Date, previousDate.Date);
		Assert.Equal(DateTime.Today, DateTime.FromOADate(collectionDate).Date);
		Assert.Equal(usages.Sum(usage => usage.Quantity), worksheet.Cell("I8").GetDouble());
	}

	[Fact]
	public async Task RenderAsync_NullOutput_ThrowsArgumentNullException()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await _renderer.RenderAsync(null, Templates.ApartmentUsage.Template, Templates.ApartmentUsage));
	}

	[Fact]
	public async Task RenderAsync_NullTemplate_ThrowsArgumentNullException()
	{
		using var output = new MemoryStream();
		await Assert.ThrowsAsync<ArgumentNullException>(async () => await _renderer.RenderAsync(output, null, Templates.ApartmentUsage));
	}

	[Fact]
	public async Task RenderAsync_UnsupportedTemplateFormat_ThrowsInvalidOperationException()
	{
		using var output = new MemoryStream();
		var template = new UnsupportedTemplate();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await _renderer.RenderAsync(output, template, Templates.ApartmentUsage));

		Assert.Contains("Unsupported template format", exception.Message);
		Assert.Equal(0, output.Length);
	}

	[Fact]
	public async Task RenderAsync_SupportedRenderingFormat_RendersWorkbook()
	{
		using var output = new MemoryStream();

		await _renderer.RenderAsync(output, Templates.ApartmentUsage.Template, Templates.ApartmentUsage, Spreadsheet.Format.Name);

		output.Position = 0;
		using var workbook = new XLWorkbook(output);
		var worksheet = Assert.Single(workbook.Worksheets);
		Assert.Equal(Templates.ApartmentUsage.Park.Name, worksheet.Cell("A1").GetString());
	}

	[Fact]
	public async Task RenderAsync_UnsupportedRenderingFormat_ThrowsInvalidOperationException()
	{
		using var output = new MemoryStream();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await _renderer.RenderAsync(output, Templates.ApartmentUsage.Template, Templates.ApartmentUsage, "PDF"));

		Assert.Contains("Unsupported rendering format", exception.Message);
		Assert.Equal(0, output.Length);
	}

	private sealed class UnsupportedTemplate : IDataTemplate
	{
		public string Name => "unsupported";
		public DataArchiveFormat Format { get; } = new("PDF", "application/pdf", ".pdf");
		public string Title { get; set; }
		public string Description { get; set; }
		public Stream Open() => throw new InvalidOperationException("The unsupported template must not be opened.");
	}
}
