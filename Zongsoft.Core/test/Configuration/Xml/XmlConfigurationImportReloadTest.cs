using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Zongsoft.Configuration.Xml;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class XmlConfigurationImportReloadTest
{
	[Fact]
	public void Reload_RebuildsTransitiveGraphAndReleasesObsoleteWatches()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?import path='child.option'?><configuration><option><local value='root'/></option></configuration>");
		files.Set("child.option", "<?import path='leaf.option'?><configuration><option><branch value='child'/></option></configuration>");
		files.Set("leaf.option", "<?meta original?><configuration><option><item value='one'/></option></configuration>");
		files.Set("replacement.option", "<?meta replacement?><configuration><option><item value='replacement'/></option></configuration>");

		using(var configuration = Build(files))
		{
			var provider = GetProvider(configuration);
			var original = provider.Instructions;
			Assert.Equal(["child.option", "leaf.option", "root.option"], files.ActivePaths);
			Assert.Equal("one", configuration["item:value"]);
			var changed = configuration.GetReloadToken();

			files.Change("leaf.option", "<?meta updated?><configuration><option><item value='two'/></option></configuration>");

			Assert.True(changed.HasChanged);
			Assert.Equal("two", configuration["item:value"]);
			Assert.Equal("root", configuration["local:value"]);
			Assert.Equal("child", configuration["branch:value"]);
			Assert.Equal(2, files.GetReadCount("root.option"));
			Assert.Equal(2, files.GetReadCount("child.option"));
			Assert.Equal(2, files.GetReadCount("leaf.option"));
			Assert.Equal("original", original[^1].Data);
			Assert.Equal("updated", provider.Instructions[^1].Data);

			files.Change("child.option", "<?import path='replacement.option'?><configuration />");

			Assert.Equal("replacement", configuration["item:value"]);
			Assert.Null(configuration["branch:value"]);
			Assert.Equal(["child.option", "replacement.option", "root.option"], files.ActivePaths);
			Assert.Equal(3, files.GetReadCount("root.option"));
			Assert.Equal(1, files.GetReadCount("replacement.option"));

			files.Change("leaf.option", "<configuration />");

			Assert.Equal(3, files.GetReadCount("root.option"));
			Assert.Equal("replacement", configuration["item:value"]);
		}

		Assert.Empty(files.ActivePaths);
	}

	[Fact]
	public void Reload_WatchesOptionalMissingAndNoMatchButNotSkippedDependencies()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<configuration><option path='Selected'><?import path='optional.option' required='false'?><?import path='nomatch.option'?></option></configuration>");
		files.Set("nomatch.option", "<?meta no-match?><configuration><option path='Other'><?import path='skipped.option'?></option></configuration>");
		files.Set("skipped.option", "<configuration />");
		using var configuration = Build(files);

		Assert.Equal(["nomatch.option", "optional.option", "root.option"], files.ActivePaths);
		Assert.Equal(0, files.GetReadCount("skipped.option"));
		Assert.Null(configuration["Selected:optional:value"]);
		Assert.Equal(["path='optional.option' required='false'", "path='nomatch.option'", "no-match"], GetProvider(configuration).Instructions.Select(instruction => instruction.Data));

		files.Change("optional.option", "<configuration><option path='Selected'><optional value='appeared'/></option></configuration>");

		Assert.Equal("appeared", configuration["Selected:optional:value"]);
		Assert.Equal(2, files.GetReadCount("root.option"));

		files.Change("nomatch.option", "<configuration><option path='Selected'><found value='now-matches'/></option></configuration>");

		Assert.Equal("now-matches", configuration["Selected:found:value"]);
		Assert.Equal("appeared", configuration["Selected:optional:value"]);
		Assert.Equal(3, files.GetReadCount("root.option"));

		files.Change("skipped.option", "<configuration />");

		Assert.Equal(3, files.GetReadCount("root.option"));
		Assert.Equal(0, files.GetReadCount("skipped.option"));
	}

	[Fact]
	public void Reload_FailureClearsPublishedDataAndInstructionsAndCanRecover()
	{
		using var files = new XmlImportFiles();
		const string ROOT = "<?import path='child.option'?><configuration />";
		files.Set("root.option", ROOT);
		files.Set("child.option", "<?meta old?><configuration><option><item value='old'/></option></configuration>");
		var errors = new List<Exception>();
		using var configuration = Build(files, errors: errors);
		var provider = GetProvider(configuration);
		var snapshot = provider.Instructions;

		files.Change("child.option", "<?meta partial?><configuration><option>");

		Assert.IsType<InvalidDataException>(Assert.Single(errors));
		Assert.Null(configuration["item:value"]);
		Assert.Empty(provider.Instructions);
		Assert.Equal("old", snapshot[^1].Data);
		Assert.Equal(["child.option", "root.option"], files.ActivePaths);

		files.Change("child.option", "<?meta repaired?><configuration><option><item value='repaired'/></option></configuration>");

		Assert.Equal("repaired", configuration["item:value"]);
		Assert.Equal("repaired", provider.Instructions[^1].Data);

		files.Change("child.option", null);

		Assert.Equal(2, errors.Count);
		Assert.IsType<FileNotFoundException>(errors[^1].InnerException);
		Assert.Empty(provider.Instructions);
		Assert.Null(configuration["item:value"]);
		Assert.Equal(["child.option", "root.option"], files.ActivePaths);

		files.Change("child.option", "<configuration><option><item value='restored'/></option></configuration>");

		Assert.Equal("restored", configuration["item:value"]);

		files.Change("root.option", null);

		Assert.Empty(provider.Instructions);
		Assert.Null(configuration["item:value"]);
		Assert.Equal(["root.option"], files.ActivePaths);

		files.Change("root.option", ROOT);

		Assert.Equal("restored", configuration["item:value"]);
		Assert.Equal(["child.option", "root.option"], files.ActivePaths);
	}

	[Fact]
	public void Reload_UsesNewOptionsAndRegistrySnapshot()
	{
		using var files = new XmlImportFiles();
		var originalName = "xml_original_" + Guid.NewGuid().ToString("N");
		var lateName = "xml_late_" + Guid.NewGuid().ToString("N");
		var originalCalls = 0;
		var lateCalls = 0;
		var originalOptions = new XmlConfigurationInstructionOptions(originalName);
		var registered = false;
		XmlConfiguration.Instructions.Add(new CallbackProcessor(originalName, context =>
		{
			originalCalls++;
			originalOptions.Behavior = XmlConfigurationInstructionBehavior.Ignore;
			context.Configuration["phase"] = "original";
		}));
		var text = $"<?{originalName}?><?import path='child.option'?><configuration />";
		files.Set("root.option", text);
		files.Set("child.option", $"<?{originalName}?><?{lateName}?><configuration />");
		using var configuration = Build(files, options =>
		{
			options.Instructions.Add(originalOptions);
			options.Loading = context =>
			{
				if(registered)
					return;

				registered = true;
				XmlConfiguration.Instructions.Add(new CallbackProcessor(lateName, current =>
				{
					lateCalls++;
					current.Configuration["phase"] = "late";
				}));
			};
		});

		Assert.Equal(2, originalCalls);
		Assert.Equal(0, lateCalls);
		Assert.Equal("original", configuration["phase"]);

		files.Change("child.option", $"<?{originalName}?><?{lateName}?><configuration />");

		Assert.Equal(2, originalCalls);
		Assert.Equal(1, lateCalls);
		Assert.Equal("late", configuration["phase"]);
		Assert.Equal(4, GetProvider(configuration).Instructions.Count);
	}

	[Fact]
	public void Reload_IgnoredImportsHaveNoDependencyWatches()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?import path='ignored.option'?><configuration />");
		files.Set("ignored.option", "<configuration />");
		using var configuration = Build(files, options => options.Instructions.Add(XmlConfigurationInstructionOptions.Import(XmlConfigurationInstructionBehavior.Ignore)));

		Assert.Equal(["root.option"], files.ActivePaths);
		Assert.Equal(0, files.GetReadCount("ignored.option"));

		files.Change("ignored.option", "<configuration><option><item value='ignored'/></option></configuration>");

		Assert.Equal(1, files.GetReadCount("root.option"));
		Assert.Null(configuration["item:value"]);
	}

	private static XmlConfigurationProvider GetProvider(ConfigurationRoot configuration) => Assert.IsType<XmlConfigurationProvider>(Assert.Single(configuration.Providers));
	private static ConfigurationRoot Build(XmlImportFiles files, Action<XmlConfigurationOptions> configure = null, List<Exception> errors = null) => (ConfigurationRoot)new ConfigurationBuilder().AddOptionFile(source =>
	{
		source.FileProvider = files;
		source.Path = "root.option";
		source.ReloadOnChange = true;
		source.ReloadDelay = 0;
		source.OnLoadException = context =>
		{
			errors?.Add(context.Exception);
			context.Ignore = true;
		};
		configure?.Invoke(source.Options);
	}).Build();

	private sealed class CallbackProcessor(string name, Action<XmlConfigurationInstructionContext> action) : XmlConfigurationInstructionProcessor(name)
	{
		public override void Process(XmlConfigurationInstructionContext context) => action(context);
	}
}
