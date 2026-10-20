using System;
using System.IO;
using System.Xml.Linq;

namespace Zongsoft.Plugins.Web.Tests;

internal class PluginTestDirectory : IDisposable
{
	private readonly bool _ownsDirectory;

	public PluginTestDirectory(string directory = null)
	{
		_ownsDirectory = directory == null;
		this.DirectoryPath = directory ?? Path.Combine(Path.GetTempPath(), $"zongsoft-web-tests-{Guid.NewGuid():N}");

		if(_ownsDirectory)
		{
			Directory.CreateDirectory(Path.Combine(this.DirectoryPath, "plugins"));
			var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"));
			document.Root.Elements("extension").Remove();
			this.Write("plugins/Main.plugin", document.ToString());
		}
	}

	public string DirectoryPath { get; }

	public string Write(string path, string content)
	{
		var target = Path.GetFullPath(Path.Combine(this.DirectoryPath, path));
		Directory.CreateDirectory(Path.GetDirectoryName(target));
		File.WriteAllText(target, content.ReplaceLineEndings("\r\n"));
		return target;
	}

	public void Plugin(string name, string option = null)
	{
		this.Write($"plugins/{name}.plugin", $"<plugin name=\"{name}\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest></plugin>");
		if(option != null)
			this.Write($"plugins/{name}.option", option);
	}

	public void Dispose()
	{
		if(_ownsDirectory && Directory.Exists(this.DirectoryPath))
			Directory.Delete(this.DirectoryPath, true);
	}
}
