using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Zongsoft.Configuration.Xml;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class XmlConfigurationProcessorTest
{
	[Fact]
	public void Registry_UsesUniqueCaseInsensitiveNamesAndRootSnapshots()
	{
		var name = GetUniqueName();
		var calls = 0;
		var processor = new CallbackProcessor(name, context =>
		{
			calls++;
			context.Configuration["executed"] = "yes";
		});
		var source = new XmlStreamConfigurationSource();
		var registered = false;
		source.Options.Loading = _ =>
		{
			if(registered)
				return;

			XmlConfiguration.Instructions.Add(processor);
			registered = true;
		};
		var provider = new XmlStreamConfigurationProvider(source);
		var text = $"<?{name.ToUpperInvariant()}?><configuration />";

		Load(provider, text);

		Assert.Equal(0, calls);
		Assert.False(provider.TryGet("executed", out _));
		Assert.Same(processor, XmlConfiguration.Instructions[name.ToUpperInvariant()]);
		Assert.Throws<ArgumentException>(() => XmlConfiguration.Instructions.Add(new CallbackProcessor(name.ToUpperInvariant(), _ => { })));
		Assert.Equal("import", XmlConfiguration.Instructions["IMPORT"].Name, ignoreCase: true);

		Load(provider, text);

		Assert.Equal(1, calls);
		Assert.True(provider.TryGet("executed", out var value));
		Assert.Equal("yes", value);
		Assert.Equal(name.ToUpperInvariant(), Assert.Single(provider.Instructions).Name);
	}

	[Fact]
	public void Read_DoesNotExecuteRegisteredInstructionsOrImport()
	{
		var name = GetUniqueName();
		var calls = 0;
		XmlConfiguration.Instructions.Add(new CallbackProcessor(name, _ => calls++));
		using var stream = CreateStream($"<?import path='missing.option'?><?{name}?><configuration />");

		var data = XmlStreamConfigurationProvider.Read(stream, out var instructions);

		Assert.Empty(data);
		Assert.Equal(["import", name], instructions.Select(instruction => instruction.Name));
		Assert.Equal(0, calls);
		Assert.True(stream.CanRead);
	}

	[Fact]
	public void Processor_CurrentDataIsIndependentFromOriginalInstruction()
	{
		var name = GetUniqueName();
		var events = new List<string>();
		var source = new XmlStreamConfigurationSource();
		XmlConfiguration.Instructions.Add(new CallbackProcessor(name, context =>
		{
			events.Add("processor");
			Assert.Equal("  path='new' value='a&amp;b'  ", context.Data);
			Assert.Equal("a&b", context.GetAttributes()["VALUE"]);
			Assert.Equal("path='original'", context.Instruction.Data);
			context.Configuration["handled"] = context.GetAttributes()["path"];
		}));
		source.Options.Instructions.Processing = context =>
		{
			events.Add("processing");
			Assert.Equal("original", context.GetAttributes()["path"]);
			context.Data = "free text";
			Assert.False(context.TryGetAttributes(out var invalid));
			Assert.Null(invalid);
			context.Data = "  path='new' value='a&amp;b'  ";
			Assert.True(context.TryGetAttributes(out var attributes));
			Assert.Equal("new", attributes["PATH"]);
		};
		source.Options.Instructions.Processed = context =>
		{
			events.Add("processed");
			Assert.Equal("new", context.Configuration["handled"]);
			context.Data = "path='too-late'";
		};
		var provider = new XmlStreamConfigurationProvider(source);

		Load(provider, $"<?{name} path='original'?><configuration />");

		Assert.Equal(["processing", "processor", "processed"], events);
		Assert.Equal("path='original'", Assert.Single(provider.Instructions).Data);
		Assert.True(provider.TryGet("handled", out var value));
		Assert.Equal("new", value);
	}

	[Fact]
	public void Processor_ContextsDescribePhysicalAndOptionScopes()
	{
		var contexts = new List<XmlConfigurationInstructionContext>();
		var source = new XmlStreamConfigurationSource { FilePath = "logical/root.option" };
		source.Options.Instructions.Processing = contexts.Add;
		var provider = new XmlStreamConfigurationProvider(source);

		Load(provider, "<?custom before?><configuration><?custom root?><option path=' /A/B/ \t'><?custom option?><item><?custom nested?></item></option><option><?custom default?></option></configuration><?custom after?>");

		Assert.Equal(["before", "root", "option", "nested", "default", "after"], contexts.Select(context => context.Data));
		Assert.Equal([null, null, "A:B", "A:B", string.Empty, null], contexts.Select(context => context.OptionPath));
		Assert.Equal([string.Empty, "/configuration", "/configuration/option", "/configuration/option/item", "/configuration/option", string.Empty], contexts.Select(context => context.Instruction.Path));
		Assert.All(contexts, context =>
		{
			Assert.Equal(1, context.Depth);
			Assert.Equal("logical/root.option", context.FilePath);
			Assert.Null(context.Referer);
		});
		Assert.Equal(contexts.Select(context => context.Instruction), provider.Instructions);
	}

	[Theory]
	[InlineData(XmlConfigurationInstructionBehavior.None, false, false)]
	[InlineData(XmlConfigurationInstructionBehavior.Strict, false, true)]
	[InlineData(XmlConfigurationInstructionBehavior.Strict, true, false)]
	[InlineData(XmlConfigurationInstructionBehavior.Ignore, false, false)]
	[InlineData(XmlConfigurationInstructionBehavior.Suppress, true, true)]
	public void Processor_BehaviorsControlDispatch(XmlConfigurationInstructionBehavior behavior, bool handled, bool fails)
	{
		var name = GetUniqueName();
		var before = 0;
		var after = 0;
		var source = new XmlStreamConfigurationSource();
		source.Options.Instructions.Add(new XmlConfigurationInstructionOptions(name, behavior));
		source.Options.Instructions.Processing = context =>
		{
			before++;
			context.Handled = handled;
			Assert.Equal(behavior, context.Behavior);
		};
		source.Options.Instructions.Processed = _ => after++;
		var provider = new XmlStreamConfigurationProvider(source);
		var text = $"<?{name} arbitrary data?><configuration />";

		if(fails)
			Assert.Throws<FormatException>(() => Load(provider, text));
		else
		{
			Load(provider, text);
			Assert.Equal(name, Assert.Single(provider.Instructions).Name);
		}

		Assert.Equal(behavior is XmlConfigurationInstructionBehavior.Ignore or XmlConfigurationInstructionBehavior.Suppress ? 0 : 1, before);
		Assert.Equal(fails || behavior == XmlConfigurationInstructionBehavior.Ignore ? 0 : 1, after);
	}

	[Fact]
	public void Processor_HandledSkipsRegisteredProcessorButStillCompletesCallbacks()
	{
		var name = GetUniqueName();
		var calls = 0;
		var source = new XmlStreamConfigurationSource();
		XmlConfiguration.Instructions.Add(new CallbackProcessor(name, _ => calls++));
		source.Options.Instructions.Processing = context =>
		{
			context.Configuration["handled"] = "by-callback";
			context.Handled = true;
		};
		source.Options.Instructions.Processed = context =>
		{
			Assert.True(context.Handled);
			context.Configuration["completed"] = "yes";
		};
		var provider = new XmlStreamConfigurationProvider(source);

		Load(provider, $"<?{name}?><configuration />");

		Assert.Equal(0, calls);
		Assert.True(provider.TryGet("handled", out var handled));
		Assert.Equal("by-callback", handled);

		Assert.True(provider.TryGet("completed", out var completed));
		Assert.Equal("yes", completed);
	}

	[Fact]
	public void Processor_OptionsAreIndependentAndBehaviorIsFixed()
	{
		var name = GetUniqueName();
		var configured = new MutableOptions(name) { Values = ["original"] };
		var contexts = new List<XmlConfigurationInstructionContext>();
		var source = new XmlStreamConfigurationSource();
		source.Options.Instructions.Add(configured);
		source.Options.Instructions.Processing = context =>
		{
			contexts.Add(context);
			var options = Assert.IsType<MutableOptions>(context.Options);
			Assert.Equal(["original"], options.Values);
			options.Values.Add(context.Data);
			options.Behavior = XmlConfigurationInstructionBehavior.Suppress;
			Assert.Equal(XmlConfigurationInstructionBehavior.None, context.Behavior);
		};
		var provider = new XmlStreamConfigurationProvider(source);

		Load(provider, $"<?{name} first?><?{name} second?><configuration />");

		Assert.Equal(2, provider.Instructions.Count);
		Assert.Equal(["original"], configured.Values);
		Assert.NotSame(configured, contexts[0].Options);
		Assert.NotSame(contexts[0].Options, contexts[1].Options);
		Assert.Equal(["original", "first"], ((MutableOptions)contexts[0].Options).Values);
		Assert.Equal(["original", "second"], ((MutableOptions)contexts[1].Options).Values);
	}

	[Fact]
	public void Processor_SourceChangesApplyToNextRootOnly()
	{
		var name = GetUniqueName();
		var source = new XmlStreamConfigurationSource();
		var options = new XmlConfigurationInstructionOptions(name);
		source.Options.Instructions.Add(options);
		var events = new List<string>();
		source.Options.Instructions.Processing = context =>
		{
			events.Add("old:" + context.Data);
			options.Behavior = XmlConfigurationInstructionBehavior.Ignore;
			source.Options.Instructions.Processing = _ => events.Add("new");
			source.Options.Instructions.Processed = _ => events.Add("new-after");
		};
		source.Options.Instructions.Processed = context => events.Add("old-after:" + context.Data);
		var provider = new XmlStreamConfigurationProvider(source);
		var text = $"<?{name} first?><?{name} second?><configuration />";

		Load(provider, text);

		Assert.Equal(["old:first", "old-after:first", "old:second", "old-after:second"], events);

		events.Clear();
		Load(provider, text);

		Assert.Empty(events);
		Assert.Equal(2, provider.Instructions.Count);
	}

	[Fact]
	public void Processor_MutationsDoNotDeclareXmlKeys()
	{
		var name = GetUniqueName();
		XmlConfiguration.Instructions.Add(new CallbackProcessor(name, context => context.Configuration["Example:item:value"] = "processor"));
		var provider = new XmlStreamConfigurationProvider(new XmlStreamConfigurationSource());

		Load(provider, $"<?{name}?><configuration><option path='Example'><item value='xml' /></option></configuration>");

		Assert.True(provider.TryGet("Example:item:value", out var value));
		Assert.Equal("xml", value);

		Load(provider, $"<configuration><option path='Example'><item value='xml' /></option></configuration><?{name}?>");

		Assert.True(provider.TryGet("Example:item:value", out value));
		Assert.Equal("processor", value);
	}

	[Theory]
	[InlineData("loading")]
	[InlineData("processing")]
	[InlineData("processed")]
	[InlineData("loaded")]
	public void Processor_CallbackFailuresDoNotPublishPartialSnapshots(string phase)
	{
		var source = new XmlStreamConfigurationSource();
		var provider = new XmlStreamConfigurationProvider(source);
		Load(provider, "<?original?><configuration><option><item value='stable' /></option></configuration>");
		var snapshot = provider.Instructions;
		var failure = new InvalidOperationException("callback failure");

		switch(phase)
		{
			case "loading":
				source.Options.Loading = _ => throw failure;
				break;
			case "processing":
				source.Options.Instructions.Processing = _ => throw failure;
				break;
			case "processed":
				source.Options.Instructions.Processed = _ => throw failure;
				break;
			case "loaded":
				source.Options.Loaded = _ => throw failure;
				break;
		}

		Assert.Same(failure, Assert.Throws<InvalidOperationException>(() => Load(provider, "<?partial?><configuration><option><item value='changed' /></option></configuration>")));
		Assert.Same(snapshot, provider.Instructions);
		Assert.Equal("original", Assert.Single(snapshot).Name);
		Assert.True(provider.TryGet("item:value", out var value));
		Assert.Equal("stable", value);
	}

	private static string GetUniqueName() => "xml_test_" + Guid.NewGuid().ToString("N");
	private static MemoryStream CreateStream(string content) => new(Encoding.UTF8.GetBytes(content));
	private static void Load(XmlStreamConfigurationProvider provider, string content)
	{
		using var stream = CreateStream(content);
		provider.Load(stream);

		Assert.True(stream.CanRead);
	}

	private sealed class CallbackProcessor(string name, Action<XmlConfigurationInstructionContext> action) : XmlConfigurationInstructionProcessor(name)
	{
		public override void Process(XmlConfigurationInstructionContext context) => action(context);
	}

	private sealed class MutableOptions(string name) : XmlConfigurationInstructionOptions(name)
	{
		public List<string> Values { get; set; } = [];
		public override XmlConfigurationInstructionOptions Clone() => new MutableOptions(this.Name) { Behavior = this.Behavior, Values = [.. this.Values] };
	}
}
