using System;

using Xunit;

using Zongsoft.Text.Templating;

namespace Zongsoft.Common.Tests;

public class VariablesEnvironmentsTest
{
	[Theory]
	[InlineData(-1)]
	[InlineData(3)]
	[InlineData(int.MinValue)]
	[InlineData(int.MaxValue)]
	public void Environments_RejectsUndefinedTargets(int target)
	{
		Assert.Equal("target", Assert.Throws<ArgumentOutOfRangeException>(() => Variables.Environments((EnvironmentVariableTarget)target)).ParamName);
	}

	[Theory]
	[InlineData(EnvironmentVariableTarget.Process)]
	[InlineData(EnvironmentVariableTarget.User)]
	[InlineData(EnvironmentVariableTarget.Machine)]
	public void Lookup_RejectsNullName(EnvironmentVariableTarget target)
	{
		var variables = Variables.Environments(target);

		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(string.Empty, null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue("env", null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, true, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue("env", null, true, out _)).ParamName);
	}

	[Fact]
	public void ProcessView_ReflectsAddReplaceAndRemove()
	{
		var name = CreateName();
		var variables = Variables.Environments();

		try
		{
			Assert.False(variables.TryGetValue(name, out var value));
			Assert.Null(value);
			Environment.SetEnvironmentVariable(name, "${unchanged}/ value ");
			Assert.True(variables.TryGetValue(name, out value));
			Assert.Equal("${unchanged}/ value ", value);
			Environment.SetEnvironmentVariable(name, "updated");
			Assert.True(variables.TryGetValue(name, out value));
			Assert.Equal("updated", value);
			Environment.SetEnvironmentVariable(name, null);
			Assert.False(variables.TryGetValue(name, out value));
			Assert.Null(value);
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[Fact]
	public void Lookup_FallbackControlsGlobalNamespaceWithoutChangingNames()
	{
		var name = CreateName();
		var variables = Variables.Environments();

		try
		{
			Environment.SetEnvironmentVariable(name, "value");
			Assert.True(variables.TryGetValue(null, name, out var value));
			Assert.Equal("value", value);
			Assert.True(variables.TryGetValue(string.Empty, name, out value));
			Assert.Equal("value", value);
			Assert.False(variables.TryGetValue("env", name, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue(" ", name, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue($" {name} ", out value));
			Assert.Null(value);

			foreach(var scope in new[] { "env", "app.worker", "default", "*", " " })
			{
				Assert.False(variables.TryGetValue(scope, name, false, out value));
				Assert.Null(value);
				Assert.True(variables.TryGetValue(scope, name, true, out value));
				Assert.Equal("value", value);
			}

			Assert.True(variables.TryGetValue(name, true, out value));
			Assert.Equal("value", value);
			Assert.False(variables.TryGetValue("env", $" {name} ", true, out value));
			Assert.Null(value);

		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[Fact]
	public void Lookup_UsesPlatformCaseSensitivity()
	{
		var upper = CreateName().ToUpperInvariant();
		var lower = upper.ToLowerInvariant();
		var variables = Variables.Environments();

		try
		{
			Environment.SetEnvironmentVariable(upper, "upper");
			Assert.True(variables.TryGetValue(upper, out var value));
			Assert.Equal("upper", value);

			if(OperatingSystem.IsWindows())
			{
				Assert.True(variables.TryGetValue(lower, out value));
				Assert.Equal("upper", value);
				Environment.SetEnvironmentVariable(lower, "updated");
				Assert.True(variables.TryGetValue(upper, out value));
				Assert.Equal("updated", value);
			}
			else
			{
				Assert.False(variables.TryGetValue(lower, out value));
				Assert.Null(value);
				Environment.SetEnvironmentVariable(lower, "lower");
				Assert.True(variables.TryGetValue(upper, out value));
				Assert.Equal("upper", value);
				Assert.True(variables.TryGetValue(lower, out value));
				Assert.Equal("lower", value);
			}
		}
		finally
		{
			Environment.SetEnvironmentVariable(upper, null);
			Environment.SetEnvironmentVariable(lower, null);
		}
	}

	[Theory]
	[InlineData(EnvironmentVariableTarget.User)]
	[InlineData(EnvironmentVariableTarget.Machine)]
	public void TargetViews_DoNotFallbackToProcess(EnvironmentVariableTarget target)
	{
		var name = CreateName();
		var variables = Variables.Environments(target);

		try
		{
			Environment.SetEnvironmentVariable(name, "process only");
			Assert.True(Variables.Environments().TryGetValue(name, out var value));
			Assert.Equal("process only", value);
			Assert.False(variables.TryGetValue(name, out value));
			Assert.Null(value);
			Assert.False(variables.TryGetValue("env", name, true, out value));
			Assert.Null(value);
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[Fact]
	public void ProcessView_EmptyValueMatchesRuntimeSupport()
	{
		var name = CreateName();
		var variables = Variables.Environments();

		try
		{
			Environment.SetEnvironmentVariable(name, string.Empty);
			var exists = Environment.GetEnvironmentVariable(name) != null;

			Assert.Equal(exists, variables.TryGetValue(name, out var value));

			if(exists)
				Assert.Equal(string.Empty, value);
			else
				Assert.Null(value);
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	[Fact]
	public void TemplateEvaluation_UsesEnvironmentChangesAndDefaultNamespaceOnly()
	{
		var name = CreateName();
		var fallback = new Variables { [name] = "fallback", [$"env:{name}"] = "scoped" };
		var evaluator = new TemplateEvaluator { Providers = { Variables.Environments(), fallback } };
		var template = "${" + name + "}/${env:" + name + "}";

		try
		{
			Assert.Equal("fallback/scoped", evaluator.Evaluate(template));
			Environment.SetEnvironmentVariable(name, "first");
			Assert.Equal("first/scoped", evaluator.Evaluate(template));
			Environment.SetEnvironmentVariable(name, "updated");
			Assert.Equal("updated/scoped", evaluator.Evaluate(template));
			Environment.SetEnvironmentVariable(name, null);
			Assert.Equal("fallback/scoped", evaluator.Evaluate(template));
		}
		finally
		{
			Environment.SetEnvironmentVariable(name, null);
		}
	}

	private static string CreateName() => $"ZONGSOFT_VARIABLES_{Guid.NewGuid():N}";
}
