using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Zongsoft.Configuration.Xml;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class XmlConfigurationImportTest
{
	[Fact]
	public void Import_FileAndOptionPositionsMergeInDocumentOrder()
	{
		using var files = new XmlImportFiles();
		files.Set("before.option", "<configuration><option path='Example'><before value='kept'/><item value='before'/></option></configuration>");
		files.Set("inside.option", "<configuration><option path='Example'><inside value='kept'/><item value='inside'/></option><option path='Other'><unselected value='no'/></option></configuration>");
		files.Set("after.option", "<configuration><option path='Example'><after value='kept'/><item value='after'/></option></configuration>");
		files.Set("root.option", "<?import path='before.option'?><configuration><option path='Example'><item value='root'/><?import path='inside.option'?><tail value='root-tail'/></option></configuration><?import path='after.option'?>");

		using var configuration = Build(files);
		var provider = GetProvider(configuration);

		Assert.Equal("after", configuration["Example:item:value"]);
		Assert.Equal("kept", configuration["Example:before:value"]);
		Assert.Equal("kept", configuration["Example:inside:value"]);
		Assert.Equal("kept", configuration["Example:after:value"]);
		Assert.Equal("root-tail", configuration["Example:tail:value"]);
		Assert.Null(configuration["Other:unselected:value"]);
		Assert.Equal(["path='before.option'", "path='inside.option'", "path='after.option'"], provider.Instructions.Select(instruction => instruction.Data));
		Assert.Equal([string.Empty, "/configuration/option", string.Empty], provider.Instructions.Select(instruction => instruction.Path));
	}

	[Fact]
	public void Import_OptionSelectionNormalizesExactPathsAndTraversesFileImports()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<configuration><option path=' /A/B/ \t'><local value='before'/><?import path='child.option'?><tail value='after'/></option></configuration>");
		files.Set("child.option", "<?meta child-file?><?import path='grand.option'?><configuration><option path='a/b'><first value='one'/><?meta selected?><?import path='leaf.option'?></option><option path='A/B/Child'><?meta skipped-child?><?import path='never-child.option'?></option><option path='Other'><?meta skipped-other?><?import path='never-other.option'?></option><option path='A:B'><second value='two'/><?meta second?></option></configuration><?meta child-tail?>");
		files.Set("grand.option", "<?meta grand-file?><configuration><option path='/a/B/'><grand value='yes'/><?meta grand-selected?></option><option path='Other'><?import path='never-grand.option'?></option></configuration>");
		files.Set("leaf.option", "<?meta leaf-file?><configuration><option path='A/B'><leaf value='yes'/></option></configuration>");

		using var configuration = Build(files);
		var instructions = GetProvider(configuration).Instructions;

		Assert.Equal("before", configuration["A:B:local:value"]);
		Assert.Equal("after", configuration["A:B:tail:value"]);
		Assert.Equal("one", configuration["A:B:first:value"]);
		Assert.Equal("two", configuration["A:B:second:value"]);
		Assert.Equal("yes", configuration["A:B:grand:value"]);
		Assert.Equal("yes", configuration["A:B:leaf:value"]);
		Assert.Equal([
			"path='child.option'", "child-file", "path='grand.option'", "grand-file", "grand-selected",
			"selected", "path='leaf.option'", "leaf-file", "second", "child-tail",
		], instructions.Select(instruction => instruction.Data));
		Assert.Equal(["root.option", "child.option", "child.option", "grand.option", "grand.option", "child.option", "child.option", "leaf.option", "child.option", "child.option"], instructions.Select(instruction => instruction.FilePath));
		Assert.Equal(0, files.GetReadCount("never-child.option"));
		Assert.Equal(0, files.GetReadCount("never-other.option"));
		Assert.Equal(0, files.GetReadCount("never-grand.option"));
		Assert.False(configuration.GetSection("A:B:Child").Exists());
		Assert.False(configuration.GetSection("Other").Exists());
	}

	[Theory]
	[InlineData("")]
	[InlineData(" path='' ")]
	[InlineData(" path='/' ")]
	[InlineData(" path=' / \t' ")]
	public void Import_RootSelectionMatchesMissingEmptyAndSlashPaths(string attribute)
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", $"<configuration><option{attribute}><?import path='child.option'?></option></configuration>");
		files.Set("child.option", "<configuration><option><one value='1'/></option><option path=''><two value='2'/></option><option path='/'><three value='3'/></option><option path='Other'><?import path='never.option'?></option></configuration>");

		using var configuration = Build(files);

		Assert.Equal("1", configuration["one:value"]);
		Assert.Equal("2", configuration["two:value"]);
		Assert.Equal("3", configuration["three:value"]);
		Assert.Equal(0, files.GetReadCount("never.option"));
	}

	[Fact]
	public void Import_NoMatchHasNoConfigurationOrSkippedInstructions()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<configuration><option path='Missing'><?import path='child.option'?></option></configuration>");
		files.Set("child.option", "<?meta file-level?><configuration><option path='Other'><?meta skipped?><?import path='missing-dependency.option'?><item value='wrong'/></option></configuration>");
		var completed = new List<XmlConfigurationContext>();

		using var configuration = Build(files, options => options.Loaded = completed.Add);

		Assert.Empty(Assert.Single(completed, context => context.Depth == 2).Configuration);
		Assert.Equal(["path='child.option'", "file-level"], GetProvider(configuration).Instructions.Select(instruction => instruction.Data));
		Assert.Null(configuration["Other:item:value"]);
		Assert.Equal(1, files.GetReadCount("child.option"));
		Assert.Equal(0, files.GetReadCount("missing-dependency.option"));
	}

	[Fact]
	public void Import_CallbacksObserveIndependentFilesAndLoadedChangesBeforeMerge()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?meta root-before?><configuration><option><parent value='root'/></option></configuration><?import path='child.option'?><?meta root-after?>");
		files.Set("child.option", "<?meta child?><configuration><option><child value='original'/></option></configuration>");
		var events = new List<string>();
		XmlConfigurationContext root = null;
		XmlConfigurationInstruction referer = null;
		var loading = new List<XmlConfigurationContext>();

		using var configuration = Build(files, options =>
		{
			options.Loading = context =>
			{
				Assert.Empty(context.Configuration);
				loading.Add(context);
				events.Add("loading:" + context.FilePath);

				if(context.Depth == 1)
				{
					root = context;
					Assert.Null(context.Referer);
				}
				else
					Assert.Same(referer, context.Referer);
			};
			options.Instructions.Processing = context =>
			{
				events.Add("before:" + context.FilePath + ":" + context.Instruction.Name);

				if(context.Instruction.Name == "import")
				{
					referer = context.Instruction;
					Assert.Equal("root", context.Configuration["parent:value"]);
				}
				else if(context.Depth == 2)
					Assert.False(context.Configuration.ContainsKey("parent:value"));
			};
			options.Instructions.Processed = context =>
			{
				events.Add("after:" + context.FilePath + ":" + context.Instruction.Name);

				if(context.Instruction.Name == "import")
					Assert.Equal("loaded-child", context.Configuration["child:value"]);
			};
			options.Loaded = context =>
			{
				events.Add("loaded:" + context.FilePath);

				if(context.Depth == 2)
				{
					Assert.Equal("original", context.Configuration["child:value"]);
					Assert.False(context.Configuration.ContainsKey("parent:value"));
					Assert.False(root.Configuration.ContainsKey("child:value"));
					context.Configuration["child:value"] = "loaded-child";
				}
				else
					context.Configuration["root-loaded"] = "yes";
			};
		});

		Assert.Equal([
			"loading:root.option", "before:root.option:meta", "after:root.option:meta", "before:root.option:import",
			"loading:child.option", "before:child.option:meta", "after:child.option:meta", "loaded:child.option",
			"after:root.option:import", "before:root.option:meta", "after:root.option:meta", "loaded:root.option",
		], events);
		Assert.Equal([1, 2], loading.Select(context => context.Depth));
		Assert.NotSame(loading[0].Configuration, loading[1].Configuration);
		Assert.Equal("loaded-child", configuration["child:value"]);
		Assert.Equal("root", configuration["parent:value"]);
		Assert.Equal("yes", configuration["root-loaded"]);
		Assert.Equal(["root-before", "path='child.option'", "child", "root-after"], GetProvider(configuration).Instructions.Select(instruction => instruction.Data));
	}

	[Theory]
	[InlineData("", XmlConfigurationInstructionBehavior.None, true)]
	[InlineData(" required='false'", XmlConfigurationInstructionBehavior.None, false)]
	[InlineData(" required='true'", XmlConfigurationInstructionBehavior.None, true)]
	[InlineData(" required='false'", XmlConfigurationInstructionBehavior.Strict, true)]
	[InlineData(" required='invalid'", XmlConfigurationInstructionBehavior.Strict, true)]
	public void Import_RequiredAndBehaviorRules(string required, XmlConfigurationInstructionBehavior behavior, bool missingFails)
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", $"<?import path='missing.option'{required}?><configuration><option><item value='root'/></option></configuration>");
		var loading = new List<string>();
		Action<XmlConfigurationOptions> configure = options =>
		{
			options.Instructions.Add(XmlConfigurationInstructionOptions.Import(behavior));
			options.Loading = context => loading.Add(context.FilePath);
		};

		if(missingFails)
		{
			var error = Assert.Throws<InvalidDataException>(() => Build(files, configure));
			Assert.IsAssignableFrom<FileNotFoundException>(error.InnerException);
		}
		else
		{
			using var configuration = Build(files, configure);
			Assert.Equal("root", configuration["item:value"]);
			Assert.Single(GetProvider(configuration).Instructions);
		}

		Assert.Equal(["root.option"], loading);
	}

	[Fact]
	public void Import_ProcessingRewritesCurrentDataWithoutChangingMetadata()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?import path='missing.option'?><configuration />");
		files.Set("child.option", "<configuration><option><item value='rewritten'/></option></configuration>");
		XmlConfigurationInstruction original = null;
		XmlConfigurationInstruction referer = null;

		using var configuration = Build(files, options =>
		{
			options.Instructions.Processing = context =>
			{
				original = context.Instruction;
				context.Data = "  path='child.option'  ";
			};
			options.Instructions.Processed = context => context.Data = "path='too-late.option'";
			options.Loading = context =>
			{
				if(context.Depth == 2)
					referer = context.Referer;
			};
		});

		Assert.Equal("rewritten", configuration["item:value"]);
		Assert.Equal(1, files.GetReadCount("child.option"));
		Assert.Equal(0, files.GetReadCount("missing.option"));
		Assert.Equal(0, files.GetReadCount("too-late.option"));
		Assert.Same(original, referer);
		Assert.Same(original, Assert.Single(GetProvider(configuration).Instructions));
		Assert.Equal("path='missing.option'", original.Data);
	}

	[Theory]
	[InlineData(XmlConfigurationInstructionBehavior.Ignore)]
	[InlineData(XmlConfigurationInstructionBehavior.Suppress)]
	public void Import_IgnoreAndSuppressDoNotReadOrNotify(XmlConfigurationInstructionBehavior behavior)
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?import free-form invalid attributes?><configuration />");
		var before = 0;
		var after = 0;
		Action<XmlConfigurationOptions> configure = options =>
		{
			options.Instructions.Add(XmlConfigurationInstructionOptions.Import(behavior));
			options.Instructions.Processing = _ => before++;
			options.Instructions.Processed = _ => after++;
		};

		if(behavior == XmlConfigurationInstructionBehavior.Suppress)
			Assert.IsType<FormatException>(Assert.Throws<InvalidDataException>(() => Build(files, configure)).InnerException);
		else
		{
			using var configuration = Build(files, configure);
			Assert.Equal("free-form invalid attributes", Assert.Single(GetProvider(configuration).Instructions).Data);
		}

		Assert.Equal(0, before);
		Assert.Equal(0, after);
		Assert.Equal(1, files.GetReadCount("root.option"));
	}

	[Theory]
	[InlineData("")]
	[InlineData("path='' ")]
	[InlineData("path='child.option' required='invalid'")]
	[InlineData("path='child.option' PATH='duplicate.option'")]
	public void Import_InvalidParametersFail(string data)
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", $"<?import {data}?><configuration />");
		files.Set("child.option", "<configuration />");

		var error = Assert.Throws<InvalidDataException>(() => Build(files));

		Assert.IsType<FormatException>(error.InnerException);
		Assert.Equal(0, files.GetReadCount("child.option"));
	}

	[Theory]
	[InlineData("<configuration><?import path='child.option'?></configuration>")]
	[InlineData("<configuration><option><parent><?import path='child.option'?></parent></option></configuration>")]
	public void Import_RejectsInvalidLocations(string xml)
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", xml);
		files.Set("child.option", "<configuration />");

		var error = Assert.Throws<InvalidDataException>(() => Build(files));

		Assert.IsType<FormatException>(error.InnerException);
		Assert.Equal(0, files.GetReadCount("child.option"));
	}

	[Fact]
	public void Import_CrossFileValuesOverrideButLocalDeclarationsRemainUnique()
	{
		using var files = new XmlImportFiles();
		files.Set("child.option", "<configuration><option path='Example'><item value='child'/></option></configuration>");
		files.Set("root.option", "<?import path='child.option'?><configuration><option path='Example'><item value='local'/></option></configuration>");

		using(var configuration = Build(files))
			Assert.Equal("local", configuration["Example:item:value"]);

		files.Set("root.option", "<configuration><option path='Example'><item value='first'/><?import path='child.option'?><item value='second'/></option></configuration>");
		var error = Assert.Throws<InvalidDataException>(() => Build(files));
		Assert.IsType<FormatException>(error.InnerException);
	}

	[Fact]
	public void Import_PhysicalPathsUseActualFileStreamAndDeclaringDirectory()
	{
		using var files = new XmlImportPhysicalFiles();
		var root = files.Write("actual/root.option", "<?import path='nested/child.option'?><configuration />");
		var child = files.Write("actual/nested/child.option", "<?import path='../shared/leaf.option'?><configuration />");
		var leaf = files.Write("actual/shared/leaf.option", "<?meta leaf?><configuration><option><item value='resolved'/></option></configuration>");
		using var stream = File.OpenRead(root);
		var provider = new XmlStreamConfigurationProvider(new XmlStreamConfigurationSource { FilePath = "diagnostic-only.option" });

		provider.Load(stream);

		Assert.True(provider.TryGet("item:value", out var value));
		Assert.Equal("resolved", value);
		Assert.Equal(["diagnostic-only.option", child, leaf], provider.Instructions.Select(instruction => instruction.FilePath));
		Assert.True(stream.CanRead);
	}

	[Fact]
	public void Import_ProviderPhysicalPathResolvesWrappedStreams()
	{
		using var physical = new XmlImportPhysicalFiles();
		const string XML = "<?import path='child.option'?><configuration />";
		var root = physical.Write("actual/root.option", XML);
		var child = physical.Write("actual/child.option", "<?meta physical-child?><configuration><option><item value='physical'/></option></configuration>");
		using var files = new XmlImportFiles();
		files.Set("logical/root.option", XML);
		files.SetPhysicalPath("logical/root.option", root);
		files.Set("logical/child.option", "<?meta logical-child?><configuration><option><item value='logical'/></option></configuration>");
		using var provider = new XmlConfigurationProvider(new XmlConfigurationSource { FileProvider = files, Path = "logical/root.option" });
		using var stream = files.GetFileInfo("logical/root.option").CreateReadStream();
		Assert.IsType<MemoryStream>(stream);

		provider.Load(stream);

		Assert.True(provider.TryGet("item:value", out var value));
		Assert.Equal("physical", value);
		Assert.Equal([root, child], provider.Instructions.Select(instruction => instruction.FilePath));
		Assert.Equal(["path='child.option'", "physical-child"], provider.Instructions.Select(instruction => instruction.Data));
		Assert.Equal(1, files.GetReadCount("logical/root.option"));
		Assert.Equal(0, files.GetReadCount("logical/child.option"));
		Assert.True(stream.CanRead);
	}

	[Fact]
	public void Import_AnonymousStreamsRequireAbsolutePaths()
	{
		using var files = new XmlImportPhysicalFiles();
		var child = files.Write("child.option", "<configuration><option><item value='absolute'/></option></configuration>");
		var provider = new XmlStreamConfigurationProvider(new XmlStreamConfigurationSource { FilePath = files.GetPath("root.option") });

		using(var relative = new MemoryStream(Encoding.UTF8.GetBytes("<?import path='child.option' required='false'?><configuration />")))
			Assert.Throws<FormatException>(() => provider.Load(relative));

		using var absolute = new MemoryStream(Encoding.UTF8.GetBytes($"<?import path='{Escape(child)}'?><configuration />"));
		provider.Load(absolute);

		Assert.True(provider.TryGet("item:value", out var value));
		Assert.Equal("absolute", value);
		Assert.True(absolute.CanRead);
	}

	[Fact]
	public void Import_VirtualSourcesResolveLogicalPathsAndRejectPhysicalPaths()
	{
		using var files = new XmlImportFiles();
		files.Set("folder/root.option", "<?import path='nested/child.option'?><configuration />");
		files.Set("folder/nested/child.option", "<?import path='../leaf.option'?><configuration />");
		files.Set("folder/leaf.option", "<configuration><option><item value='logical'/></option></configuration>");

		using(var configuration = Build(files, path: "folder/root.option"))
			Assert.Equal("logical", configuration["item:value"]);

		using var physical = new XmlImportPhysicalFiles();
		var target = physical.Write("actual.option", "<configuration />");
		files.Set("root.option", $"<?import path='{Escape(target)}' required='false'?><configuration />");
		Assert.IsType<FormatException>(Assert.Throws<InvalidDataException>(() => Build(files)).InnerException);
	}

	[Fact]
	public void Import_ActiveCyclesFailButDiamondAndRepeatedFilesReload()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?import path='left.option'?><?import path='right.option'?><?import path='leaf.option'?><configuration />");
		files.Set("left.option", "<?import path='leaf.option'?><configuration />");
		files.Set("right.option", "<?import path='leaf.option'?><configuration />");
		files.Set("leaf.option", "<?meta leaf?><configuration><option><item value='leaf'/></option></configuration>");

		using(var configuration = Build(files))
		{
			Assert.Equal("leaf", configuration["item:value"]);
			Assert.Equal(3, files.GetReadCount("leaf.option"));
			Assert.Equal(3, GetProvider(configuration).Instructions.Count(instruction => instruction.Name == "meta"));
		}

		files.Set("leaf.option", "<?import path='./root.option'?><configuration />");
		var loading = new List<string>();
		var error = Assert.Throws<InvalidDataException>(() => Build(files, options => options.Loading = context => loading.Add(context.FilePath)));
		Assert.IsType<FormatException>(error.InnerException);
		Assert.Equal(["root.option", "left.option", "leaf.option"], loading);
	}

	[Theory]
	[InlineData(2, 2, true)]
	[InlineData(2, 3, false)]
	[InlineData(64, 64, true)]
	[InlineData(64, 65, false)]
	public void Import_DepthBoundaryCountsRoot(int maximumDepth, int length, bool succeeds)
	{
		using var files = new XmlImportFiles();

		for(var index = 1; index <= length; index++)
			files.Set(index + ".option", index == length ? "<configuration><option><item value='complete'/></option></configuration>" : $"<?import path='{index + 1}.option'?><configuration />");

		var depths = new List<int>();
		Action<XmlConfigurationOptions> configure = options =>
		{
			if(maximumDepth != 64)
				options.Instructions.Add(XmlConfigurationInstructionOptions.Import(maximumDepth: maximumDepth));

			options.Loading = context => depths.Add(context.Depth);
		};

		if(succeeds)
		{
			using var configuration = Build(files, configure, "1.option");
			Assert.Equal("complete", configuration["item:value"]);
		}
		else
			Assert.IsType<FormatException>(Assert.Throws<InvalidDataException>(() => Build(files, configure, "1.option")).InnerException);

		Assert.Equal(Enumerable.Range(1, Math.Min(length, maximumDepth)), depths);
	}

	[Fact]
	public void Import_LinkAliasesShareActiveIdentity()
	{
		using var files = new XmlImportPhysicalFiles();
		var root = files.Write("root.option", "<?import path='alias/root.option'?><configuration />");
		files.Link("alias");
		var loaded = new List<string>();
		var source = new XmlStreamConfigurationSource();
		source.Options.Loading = context => loaded.Add(context.FilePath);
		source.Options.Instructions.Add(XmlConfigurationInstructionOptions.Import(maximumDepth: 3));
		var provider = new XmlStreamConfigurationProvider(source);

		using var stream = File.OpenRead(root);
		Assert.Throws<FormatException>(() => provider.Load(stream));

		Assert.Equal([root], loaded);
		Assert.Empty(provider.Instructions);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void Import_InvalidDepthIsRejectedBeforeLoading(int depth)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => XmlConfigurationInstructionOptions.Import(maximumDepth: depth));
	}

	[Fact]
	public void Import_OptionalDoesNotHideParsingOrCallbackFailures()
	{
		using var files = new XmlImportFiles();
		files.Set("root.option", "<?import path='child.option' required='false'?><configuration />");
		files.Set("child.option", "<configuration>");
		Assert.IsType<System.Xml.XmlException>(Assert.Throws<InvalidDataException>(() => Build(files)).InnerException);

		files.Set("child.option", "<configuration />");
		var openingFailure = new IOException("optional import cannot hide read failures");
		files.FailOpening("child.option", openingFailure);
		Assert.Same(openingFailure, Assert.Throws<InvalidDataException>(() => Build(files)).InnerException);
		files.FailOpening("child.option", null);

		var failure = new FileNotFoundException("callback failure is not an optional absent import");
		var error = Assert.Throws<InvalidDataException>(() => Build(files, options => options.Loaded = context =>
		{
			if(context.Depth == 2)
				throw failure;
		}));
		Assert.Same(failure, error.InnerException);
	}

	private static string Escape(string value) => value.Replace("&", "&amp;").Replace("'", "&apos;");
	private static XmlConfigurationProvider GetProvider(ConfigurationRoot configuration) => Assert.IsType<XmlConfigurationProvider>(Assert.Single(configuration.Providers));
	private static ConfigurationRoot Build(XmlImportFiles files, Action<XmlConfigurationOptions> configure = null, string path = "root.option") => (ConfigurationRoot)new ConfigurationBuilder().AddOptionFile(source =>
	{
		source.FileProvider = files;
		source.Path = path;
		configure?.Invoke(source.Options);
	}).Build();
}
