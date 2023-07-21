namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetRendererTest
{
	private readonly SpreadsheetRenderer _renderer = new();

	[Fact]
	public void TestRender()
	{
		var path = Path.Combine(Environment.CurrentDirectory, $"{Templates.User.Template.Name}.xlsx");
		using var output = File.Open(path, FileMode.Create, FileAccess.ReadWrite);

		_renderer.RenderAsync(output, Templates.User.Template, Templates.User.Data).AsTask().Wait();
		Assert.True(output.Length > 0);

		output.Flush();
		output.Close();
	}
}