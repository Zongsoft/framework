using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Tests;

internal sealed class PluginTestScope : IDisposable
{
	private readonly IDisposable _services;

	public PluginTestScope(bool foundations = true, IDictionary<string, string> settings = null, Action<IServiceCollection> configure = null)
	{
		this.DirectoryPath = Path.Combine(Path.GetTempPath(), $"zongsoft-plugins-tests-{Guid.NewGuid():N}");
		Directory.CreateDirectory(Path.Combine(this.DirectoryPath, "plugins"));
		this.Configuration = new ConfigurationBuilder().AddInMemoryCollection(settings ?? new Dictionary<string, string>()).Build();
		this.Options = new PluginOptions(this.Configuration, this.DirectoryPath, "Development");
		this.Tree = PluginTree.Get(this.Options);

		var services = new ServiceCollection();
		services.AddSingleton<IConfiguration>(this.Configuration);
		services.AddSingleton(this.Configuration);
		configure?.Invoke(services);
		var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
		_services = (IDisposable)provider;
		this.Application = new TestApplicationContext(provider, this.Options);

		if(foundations)
		{
			//复用随产品交付的注册，避免测试维护一份不同的构建器/解析器列表。
			var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin"));
			document.Root.Elements("extension").Remove();
			this.WriteFile("Main.plugin", document.ToString());
		}
	}

	public string DirectoryPath { get; }
	public IConfigurationRoot Configuration { get; }
	public PluginOptions Options { get; }
	public PluginTree Tree { get; }
	public TestApplicationContext Application { get; }

	public string WriteFile(string path, string content)
	{
		var filePath = Path.Combine(this.DirectoryPath, "plugins", path);
		Directory.CreateDirectory(Path.GetDirectoryName(filePath));
		File.WriteAllText(filePath, content.ReplaceLineEndings("\r\n"));
		return filePath;
	}

	public void WritePlugin(string content) => this.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest>{content}</plugin>");
	public Builtin Builtin(string path) => (Builtin)this.Tree.Find(path).Value;

	public void Dispose()
	{
		try
		{
			this.Application.Dispose();
			_services.Dispose();
			(this.Configuration as IDisposable)?.Dispose();
		}
		finally
		{
			Directory.Delete(this.DirectoryPath, true);
		}
	}

	internal sealed class TestApplicationContext(IServiceProvider services, PluginOptions options) : PluginApplicationContext(services, options)
	{
		protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node) => base.CreateWorkbench(out node) ?? new TestWorkbench();
	}

	private sealed class TestWorkbench : IWorkbenchBase
	{
		public event EventHandler Opened;
		public event EventHandler Opening;
		public event EventHandler Closed;
		public event System.ComponentModel.CancelEventHandler Closing;
		public WorkbenchStatus Status { get; private set; }
		public string Title { get; set; }

		public void Open()
		{
			this.Opening?.Invoke(this, EventArgs.Empty);
			this.Status = WorkbenchStatus.Running;
			this.Opened?.Invoke(this, EventArgs.Empty);
		}

		public void Close()
		{
			var args = new System.ComponentModel.CancelEventArgs();
			this.Closing?.Invoke(this, args);
			if(args.Cancel)
				return;

			this.Status = WorkbenchStatus.None;
			this.Closed?.Invoke(this, EventArgs.Empty);
		}
	}
}
