namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetGeneratorTest
{
	private readonly SpreadsheetGenerator _generator = new();

	[Fact]
	public void TestGenerate()
	{
		var path = Path.Combine(Environment.CurrentDirectory, $"{Templates.User.Descriptor.Name}.xlsx");
		using var output = File.Open(path, FileMode.Create, FileAccess.ReadWrite);

		_generator.GenerateAsync(output, Templates.User.Descriptor, Templates.User.Data).AsTask().Wait();
		Assert.True(output.Length > 0);

		output.Flush();
		output.Close();
	}
}
