using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.ClosedXml.Tests;

public class SpreadsheetGeneratorTest
{
	private readonly SpreadsheetGenerator _generator = new();

	[Fact]
	public async Task TestGenerateAsync()
	{
		var path = Path.Combine(Environment.CurrentDirectory, $"{Templates.User.Descriptor.Name}.xlsx");
		using var output = File.Open(path, FileMode.Create, FileAccess.ReadWrite);

		await _generator.GenerateAsync(output, Templates.User.Descriptor, Templates.User.Data);
		Assert.True(output.Length > 0);

		output.Flush();
		output.Close();
	}
}
