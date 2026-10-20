using System;
using System.IO;
using System.Xml.Linq;

namespace Zongsoft.Plugins.IntegrationTests;

internal sealed class HostTestDirectory : IDisposable
{
	public HostTestDirectory()
	{
		this.DirectoryPath = Path.Combine(Path.GetTempPath(), $"zongsoft-host-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(Path.Combine(this.DirectoryPath, "plugins"));
		var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"));
		document.Root.Elements("extension").Remove();
		this.WriteFile("Main.plugin", document.ToString());
	}

	public string DirectoryPath { get; }
	public string WriteFile(string path, string content)
	{
		var target = Path.Combine(this.DirectoryPath, "plugins", path);
		Directory.CreateDirectory(Path.GetDirectoryName(target));
		File.WriteAllText(target, content.ReplaceLineEndings("\r\n"));
		return target;
	}

	public void WritePlugin(string content) => this.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest>{content}</plugin>");
	public void Dispose() => Directory.Delete(this.DirectoryPath, true);
}
