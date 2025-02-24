using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetRendererTest
{
	private readonly SpreadsheetRenderer _renderer = new();

	[Fact]
	public async Task TestRenderAsync()
	{
		var path = Path.Combine(Environment.CurrentDirectory, $"{Templates.ApartmentUsage.Template.Name}.xlsx");
		using var output = File.Open(path, FileMode.Create, FileAccess.ReadWrite);

		await _renderer.RenderAsync(output, Templates.ApartmentUsage.Template, Templates.ApartmentUsage);
		Assert.True(output.Length > 0);

		output.Flush();
		output.Close();
	}
}