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
		Assert.True(variables.TryGetValue("INSTALL_PATH", true, out value));
		Assert.Equal("first", value);
		Assert.False(variables.TryGetValue("region_path", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("region_path", true, out value));
		Assert.Equal("first default", value);
	}

	[Fact]
	public void Lookup_FallbackControlsGlobalValuesAndDeclaredDefaults()
	{
		IVariables variables = CreateOptions("options --name:provided");

		foreach(var scope in new[] { null, string.Empty, "app.worker", "default", "*", " " })
		{
			Assert.Equal(string.IsNullOrEmpty(scope), variables.TryGetValue(scope, "NAME", out var value));
			Assert.Equal(string.IsNullOrEmpty(scope) ? "provided" : null, value);
			Assert.True(variables.TryGetValue(scope, "NAME", true, out value));
			Assert.Equal("provided", value);
			Assert.False(variables.TryGetValue(scope, "count", out value));
			Assert.Null(value);
			Assert.True(variables.TryGetValue(scope, "C", true, out value));
			Assert.Equal(3, value);
		}

		Assert.True(variables.TryGetValue("name", false, out var explicitValue));
		Assert.Equal("provided", explicitValue);
		Assert.False(variables.TryGetValue("count", false, out _));
		Assert.False(variables.TryGetValue("missing", true, out _));
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
		Assert.False(variables.TryGetValue("COUNT", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("COUNT", true, out value));
		Assert.Equal(3, value);
		Assert.True(variables.TryGetValue("C", true, out value));
		Assert.Equal(3, value);
		Assert.False(variables.TryGetValue("optional", out value));
		Assert.Null(value);
		Assert.False(variables.TryGetValue("optional", true, out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("EXTRA", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("EXTRA", true, out value));
		Assert.Null(value);
		Assert.False(variables.TryGetValue("default", "EXTRA", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("install_path", out value));
		Assert.Null(value);
		Assert.Equal("ignored", options.GetValue("invalid?"));
		Assert.Equal("ignored", options.GetValue("Q"));
		Assert.False(variables.TryGetValue("q", out value));
		Assert.Null(value);
		IVariables defaults = CreateOptions("options");
		Assert.False(defaults.TryGetValue("Q", true, out value));
		Assert.Null(value);

		foreach(var name in new[] { "missing", string.Empty })
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

		Assert.False(options.TryGetValue(null, out value));
		Assert.Null(value);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, true, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue("app", null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue("app", null, true, out _)).ParamName);
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
			Assert.False(defaults.TryGetValue("name", out var value));
			Assert.Null(value);
			Assert.True(defaults.TryGetValue("name", true, out value));
			Assert.Equal("default name", value);
			Assert.True(supplied.TryGetValue("name", out value));
			Assert.Equal("provided", value);
			Assert.True(empty.TryGetValue("name", out value));
			Assert.Null(value);

			descriptor.DefaultValue = "updated default";

			Assert.True(defaults.TryGetValue("NAME", true, out value));
			Assert.Equal("updated default", value);
			Assert.True(defaults.TryGetValue("app", "N", true, out value));
			Assert.Equal("updated default", value);
			Assert.True(supplied.TryGetValue(null, "NAME", out value));
			Assert.Equal("provided", value);
			Assert.True(supplied.TryGetValue("N", out value));
			Assert.Equal("provided", value);
			Assert.True(empty.TryGetValue(null, "NAME", out value));
			Assert.Null(value);
			Assert.True(empty.TryGetValue("N", out value));
			Assert.Null(value);
			Assert.True(supplied.TryGetValue("app", "NAME", true, out value));
			Assert.Equal("provided", value);
			Assert.True(empty.TryGetValue("app", "N", true, out value));
			Assert.Null(value);
		}
		finally
		{
			descriptor.DefaultValue = original;
		}
	}

	[Theory]
	[InlineData("name", null, "n", null, "default name")]
	[InlineData("name", "", "n", "", "default name")]
	[InlineData("enabled", "false", "e", false, true)]
	[InlineData("count", "0", "c", 0, 3)]
	public void Lookup_ExplicitEmptyAndZeroValuesBlockDefaults(string name, string input, string alias, object expected, object fallback)
	{
		var cmdlet = new CommandLine.Cmdlet("options");
		cmdlet.Options.Add(new(CommandLine.CmdletOptionKind.Fully, name, input));
		IVariables variables = CreateOptions(cmdlet);

		foreach(var scope in new[] { null, string.Empty, "app.worker", "default", "*" })
		{
			Assert.True(variables.TryGetValue(scope, name, true, out var value));
			Assert.Equal(expected, value);
			Assert.True(variables.TryGetValue(scope, alias, true, out value));
			Assert.Equal(expected, value);
		}

		IVariables defaults = CreateOptions("options");
		Assert.True(defaults.TryGetValue(name, true, out var defaultValue));
		Assert.Equal(fallback, defaultValue);
		Assert.True(defaults.TryGetValue(alias, true, out defaultValue));
		Assert.Equal(fallback, defaultValue);
	}

	[Theory]
	[InlineData(typeof(int), 0)]
	[InlineData(typeof(bool), false)]
	[InlineData(typeof(string), "")]
	public void DefaultPresence_DistinguishesOmittedNullAndTypedValues(Type type, object value)
	{
		var omitted = new CommandOptionDescriptor("value", false, type);
		var explicitNull = new CommandOptionDescriptor("value", false, type, null, null);
		var namedNull = new CommandOptionDescriptor("value", false, type, null, defaultValue: null);
		var typed = new CommandOptionDescriptor("value", 'v', false, type, null, value);
		var attribute = new CommandOptionAttribute("value", type);
		var nullAttribute = new CommandOptionAttribute("value", 'v', type, null);

		Assert.False(omitted.HasDefaultValue);
		Assert.Null(omitted.DefaultValue);
		Assert.True(explicitNull.HasDefaultValue);
		Assert.Null(explicitNull.DefaultValue);
		Assert.True(namedNull.HasDefaultValue);
		Assert.Null(namedNull.DefaultValue);
		Assert.True(typed.HasDefaultValue);
		Assert.Equal(value, typed.DefaultValue);
		Assert.False(attribute.HasDefaultValue);
		Assert.Null(attribute.DefaultValue);
		Assert.True(nullAttribute.HasDefaultValue);
		Assert.Null(nullAttribute.DefaultValue);

		omitted.DefaultValue = null;
		attribute.DefaultValue = null;

		Assert.True(omitted.HasDefaultValue);
		Assert.Null(omitted.DefaultValue);
		Assert.True(attribute.HasDefaultValue);
		Assert.Null(attribute.DefaultValue);
		attribute.DefaultValue = value;
		Assert.True(attribute.HasDefaultValue);
		Assert.Equal(value, attribute.DefaultValue);
	}

	[Fact]
	public void DefaultPresence_TransfersAttributesWithoutSynthesizingTypeDefaults()
	{
		var descriptor = CommandDescriptor.Describe(typeof(OptionsCommand));
		var options = CreateOptions("options");
		IVariables variables = options;

		foreach(var name in new[] { "no-count", "no-enabled", "optional" })
		{
			Assert.False(descriptor.Options[name].HasDefaultValue);
			Assert.Null(descriptor.Options[name].DefaultValue);
			Assert.False(options.TryGetValue(name, out var value));
			Assert.Null(value);
			Assert.Throws<ArgumentException>(() => options.GetValue(name));
			Assert.False(variables.TryGetValue(name.Replace('-', '_'), true, out value));
			Assert.Null(value);
		}

		foreach(var name in new[] { "declared-null", "assigned-null" })
		{
			Assert.True(descriptor.Options[name].HasDefaultValue);
			Assert.Null(descriptor.Options[name].DefaultValue);
			Assert.True(options.TryGetValue(name, out var value));
			Assert.Null(value);
			Assert.True(variables.TryGetValue(name.Replace('-', '_'), true, out value));
			Assert.Null(value);
		}

		Assert.True(descriptor.Options["converted-count"].HasDefaultValue);
		Assert.Equal(12, Assert.IsType<int>(descriptor.Options["converted-count"].DefaultValue));
		Assert.True(variables.TryGetValue("converted_count", true, out var converted));
		Assert.Equal(12, Assert.IsType<int>(converted));
		Assert.False(variables.TryGetValue("z", true, out _));
		Assert.True(variables.TryGetValue("u", true, out var explicitNull));
		Assert.Null(explicitNull);
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
		Assert.False(options.Contains("count"));
		Assert.Equal(3, options.GetValue<int>("count"));
		Assert.True(options.TryGetValue("C", out var count));
		Assert.Equal(3, count);
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
	[CommandOption("no-count", 'z', typeof(int))]
	[CommandOption("no-enabled", typeof(bool))]
	[CommandOption("declared-null", 'u', typeof(int), null)]
	[CommandOption("assigned-null", typeof(bool), DefaultValue = null)]
	[CommandOption("converted-count", typeof(int), "12")]
	[CommandOption("install-path", typeof(string), "default path")]
	[CommandOption("region.path", typeof(string), "first default")]
	[CommandOption("region-path", typeof(string), "second default")]
	[CommandOption("invalid?", 'q', typeof(string), "invalid default")]
	private sealed class OptionsCommand : CommandBase
	{
		protected override ValueTask<object> OnExecuteAsync(object argument, CancellationToken cancellation) => ValueTask.FromResult<object>(null);
	}
}
