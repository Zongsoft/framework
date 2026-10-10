using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Text.Templating;

namespace Zongsoft.Components.Tests;

public class CommandLineVariablesTest
{
	[Fact]
	public void Options_ImplementVariablesAndIntegrateWithTemplates()
	{
		var options = CreateOptions("options --name:Alice --count:7 --optional --install-path:/opt/app");
		IVariables variables = options;
		var fallback = new Variables { ["name"] = "fallback name", ["optional"] = "fallback optional", ["app:name"] = "scoped" };
		var evaluator = new TemplateEvaluator { Providers = { options, fallback } };

		Assert.Same(options, variables);
		Assert.True(variables.TryGetValue("COUNT", out var value));
		Assert.Equal(7, Assert.IsType<int>(value));
		Assert.Equal("Alice|7||scoped|/opt/app", evaluator.Evaluate("${NAME}|${COUNT}|${optional}|${app:name}|${install_path}"));
	}

	[Theory]
	[InlineData("install-path", "install_path", true)]
	[InlineData("install.path", "install_path", true)]
	[InlineData("install_path", "install_path", true)]
	[InlineData("bad?name", "bad_name", false)]
	[InlineData("bad/name", "bad_name", false)]
	[InlineData("bad name", "bad_name", false)]
	[InlineData("9bad", "_9bad", false)]
	[InlineData("名称", "unicode_name", false)]
	[InlineData(" bad", "bad", false)]
	[InlineData("bad@name", "bad_name", false)]
	public void Lookup_NormalizesSourceNamesAndPreservesOriginalOptionAccess(string sourceName, string variableName, bool exposed)
	{
		var cmdlet = new CommandLine.Cmdlet("options");
		cmdlet.Options.Add(new(CommandLine.CmdletOptionKind.Fully, sourceName, "provided"));
		var options = CreateOptions(cmdlet);
		IVariables variables = options;

		Assert.Equal("provided", options.GetValue(sourceName));
		Assert.True(options.TryGetValue(sourceName, out var value));
		Assert.Equal("provided", value);
		Assert.Equal(exposed, variables.TryGetValue(variableName.ToUpperInvariant(), out value));
		Assert.Equal(exposed ? "provided" : null, value);
		Assert.Equal(exposed, variables.TryGetValue(null, variableName, out value));
		Assert.Equal(exposed ? "provided" : null, value);

		if(sourceName != variableName)
		{
			Assert.False(variables.TryGetValue(sourceName, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue(string.Empty, sourceName, out value));
			Assert.Null(value);
		}
	}

	[Fact]
	public void Lookup_IgnoresCaseAndResolvesMappedNamesInSourceOrder()
	{
		IVariables variables = CreateOptions("options -N:provided --COUNT:7 --CuStOm:first --custom:last -X:unknown --install_path:first --install-path:second --install.path:third");

		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Equal("provided", value);
		Assert.True(variables.TryGetValue("n", out value));
		Assert.Equal("provided", value);
		Assert.True(variables.TryGetValue("N", out value));
		Assert.Equal("provided", value);
		Assert.True(variables.TryGetValue("count", out value));
		Assert.Equal(7, value);
		Assert.True(variables.TryGetValue("CUSTOM", out value));
		Assert.Equal("last", value);
		Assert.True(variables.TryGetValue("x", out value));
		Assert.Equal("unknown", value);
		Assert.True(variables.TryGetValue("INSTALL_PATH", out value));
		Assert.Equal("first", value);
		Assert.True(variables.TryGetValue("region_path", out value));
		Assert.Equal("first default", value);
	}

	[Fact]
	public void Lookup_OnlyAllowsDefaultNamespace()
	{
		IVariables variables = CreateOptions("options --name:provided");

		Assert.True(variables.TryGetValue("name", out var value));
		Assert.Equal("provided", value);
		Assert.True(variables.TryGetValue(null, "NAME", out value));
		Assert.Equal("provided", value);
		Assert.True(variables.TryGetValue(string.Empty, "NAME", out value));
		Assert.Equal("provided", value);

		foreach(var scope in new[] { "options", "name", " " })
		{
			Assert.False(variables.TryGetValue(scope, "name", out value));
			Assert.Null(value);
		}
	}

	[Fact]
	public void Lookup_DefaultsAndNullValuesRespectSourceValidity()
	{
		var options = CreateOptions("options --name --extra -Q:ignored --install.path");
		IVariables variables = options;

		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("N", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("COUNT", out value));
		Assert.Equal(3, value);
		Assert.True(variables.TryGetValue("C", out value));
		Assert.Equal(3, value);
		Assert.True(variables.TryGetValue("optional", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("EXTRA", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("install_path", out value));
		Assert.Null(value);
		Assert.Equal("ignored", options.GetValue("invalid?"));
		Assert.Equal("ignored", options.GetValue("Q"));
		Assert.False(variables.TryGetValue("q", out value));
		Assert.Null(value);
		IVariables defaults = CreateOptions("options");
		Assert.False(defaults.TryGetValue("Q", out value));
		Assert.Null(value);

		foreach(var name in new[] { "missing", null, string.Empty })
		{
			Assert.False(options.TryGetValue(name, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue(name, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue(null, name, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue(string.Empty, name, out value));
			Assert.Null(value);
		}
	}

	[Fact]
	public void Lookup_ReflectsChangedDefaultsAfterFirstQueryWithoutReplacingExplicitValues()
	{
		var descriptor = CommandDescriptor.Describe(typeof(OptionsCommand)).Options["name"];
		var original = descriptor.DefaultValue;
		IVariables defaults = CreateOptions("options");
		IVariables supplied = CreateOptions("options --name:provided");
		IVariables empty = CreateOptions("options --name");

		try
		{
			Assert.True(defaults.TryGetValue("name", out var value));
			Assert.Equal("default name", value);
			Assert.True(supplied.TryGetValue("name", out value));
			Assert.Equal("provided", value);
			Assert.True(empty.TryGetValue("name", out value));
			Assert.Null(value);

			descriptor.DefaultValue = "updated default";

			Assert.True(defaults.TryGetValue(null, "NAME", out value));
			Assert.Equal("updated default", value);
			Assert.True(defaults.TryGetValue("N", out value));
			Assert.Equal("updated default", value);
			Assert.True(supplied.TryGetValue(null, "NAME", out value));
			Assert.Equal("provided", value);
			Assert.True(supplied.TryGetValue("N", out value));
			Assert.Equal("provided", value);
			Assert.True(empty.TryGetValue(null, "NAME", out value));
			Assert.Null(value);
			Assert.True(empty.TryGetValue("N", out value));
			Assert.Null(value);
		}
		finally
		{
			descriptor.DefaultValue = original;
		}
	}

	[Fact]
	public void OptionAccessors_UseSameCaseInsensitiveStoredValues()
	{
		var options = CreateOptions("options --name:provided --enabled:false --CuStOm:yes -X --install-path:/opt/app");

		Assert.True(options.Contains("NAME"));
		Assert.True(options.Contains("custom"));
		Assert.Equal("provided", options.GetValue("NAME"));
		Assert.Equal("provided", options.GetValue<string>("NAME"));
		Assert.Equal("yes", options.GetValue("CUSTOM"));
		Assert.False(options.Switch("ENABLED"));
		Assert.False(options.Switch("E"));
		Assert.True(options.Switch("CUSTOM"));
		Assert.True(options.Switch("x"));
		Assert.False(options.Contains("missing"));
		Assert.Equal("/opt/app", options.GetValue("INSTALL-PATH"));
		Assert.False(options.TryGetValue("install_path", out var value));
		Assert.Null(value);
	}

	private static CommandLine.CmdletOptionCollection CreateOptions(string text) => CreateOptions(CommandLine.Parse(text)[0]);
	private static CommandLine.CmdletOptionCollection CreateOptions(CommandLine.Cmdlet cmdlet) =>
		new CommandContext(new CommandExecutor(), cmdlet, new OptionsCommand(), null).Options;

	[CommandOption("name", 'n', typeof(string), "default name")]
	[CommandOption("count", 'c', typeof(int), 3)]
	[CommandOption("enabled", 'e', typeof(bool), true)]
	[CommandOption("optional", typeof(string))]
	[CommandOption("install-path", typeof(string), "default path")]
	[CommandOption("region.path", typeof(string), "first default")]
	[CommandOption("region-path", typeof(string), "second default")]
	[CommandOption("invalid?", 'q', typeof(string), "invalid default")]
	private sealed class OptionsCommand : CommandBase
	{
		protected override ValueTask<object> OnExecuteAsync(object argument, CancellationToken cancellation) => ValueTask.FromResult<object>(null);
	}
}
