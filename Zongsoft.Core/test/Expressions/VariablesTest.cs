using System;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Text.Templating;

namespace Zongsoft.Expressions.Tests;

public class VariablesTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Constructors_UseOrdinalIgnoreCase(int constructor)
	{
		var value = new object();
		var source = new Dictionary<string, object>(StringComparer.Ordinal)
		{
			["Name"] = "default",
			["App:Name"] = value,
		};
		var variables = constructor switch
		{
			0 => new Variables(),
			1 => new Variables(8),
			_ => new Variables(source),
		};

		if(constructor != 2)
		{
			foreach(var entry in source)
				variables.Add(entry.Key, entry.Value);
		}

		Assert.Same(StringComparer.OrdinalIgnoreCase, variables.Comparer);
		Assert.Equal("default", variables["NAME"]);
		Assert.Same(value, variables["app:NAME"]);
		Assert.True(variables.TryGetValue("APP", "name", out var result));
		Assert.Same(value, result);
	}

	[Fact]
	public void CollectionConstructor_CopiesEntriesAndKeepsRawValueReferences()
	{
		var value = new object();
		var source = new Dictionary<string, object> { ["Name"] = value };
		var variables = new Variables(source);

		source["Name"] = "changed";
		source.Add("later", "source only");
		variables.Add("local", "variables only");

		Assert.Same(value, variables["name"]);
		Assert.Equal("changed", source["Name"]);
		Assert.False(variables.ContainsKey("later"));
		Assert.False(source.ContainsKey("local"));
	}

	[Theory]
	[InlineData("NAME", true, "value")]
	[InlineData("nothing", true, null)]
	[InlineData("missing", false, null)]
	[InlineData("scoped", false, null)]
	public void DefaultLookup_InterfaceOverloadsAgree(string name, bool found, object expected)
	{
		IVariables provider = new Variables
		{
			["name"] = "value",
			["nothing"] = null,
			["app:scoped"] = "scoped only",
			[":name"] = "colon name",
			[":nothing"] = "colon null",
			[":missing"] = "colon missing",
			[":scoped"] = "colon scoped",
		};

		Assert.Equal(found, provider.TryGetValue(name, out var implicitValue));
		Assert.Equal(found, provider.TryGetValue(null, name, out var explicitValue));
		Assert.Equal(found, provider.TryGetValue(string.Empty, name, out var emptyNamespaceValue));
		Assert.Equal(expected, implicitValue);
		Assert.Equal(expected, explicitValue);
		Assert.Equal(expected, emptyNamespaceValue);
	}

	[Theory]
	[InlineData("APP", "NAME", true, "app")]
	[InlineData("App.Runtime", "nAmE", true, "runtime")]
	[InlineData("other", "name", true, "other")]
	[InlineData(null, "name", true, "default")]
	[InlineData("", "name", true, "default")]
	[InlineData(" \t ", "name", true, "whitespace namespace")]
	[InlineData(" app ", "name", true, "spaced namespace")]
	[InlineData("app", " name ", true, "spaced name")]
	[InlineData("absent", "name", false, null)]
	[InlineData("app", "unscopedOnly", false, null)]
	[InlineData("app", "nothing", true, null)]
	public void NamespaceLookup_IsExactAndDoesNotFallback(string scope, string name, bool found, object expected)
	{
		IVariables provider = new Variables
		{
			["name"] = "default",
			["app:name"] = "app",
			["app.runtime:name"] = "runtime",
			["other:name"] = "other",
			[":name"] = "colon key",
			[" \t :name"] = "whitespace namespace",
			[" app :name"] = "spaced namespace",
			["app: name "] = "spaced name",
			["unscopedOnly"] = "default only",
			["app:nothing"] = null,
		};

		Assert.Equal(found, provider.TryGetValue(scope, name, out var value));
		Assert.Equal(expected, value);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("app")]
	public void NamespaceLookup_RejectsNullName(string scope)
	{
		var variables = new Variables { ["app:"] = "empty name" };

		var error = Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(scope, null, out _));

		Assert.Equal("name", error.ParamName);
	}

	[Fact]
	public void TemplateEvaluation_ProviderOrderAndNullValuesBlockFallback()
	{
		var primary = new Variables
		{
			["name"] = "first",
			["app:name"] = "first scoped",
			["nothing"] = null,
			["app:nothing"] = null,
		};
		var fallback = new Variables
		{
			["name"] = "second",
			["app:name"] = "second scoped",
			["nothing"] = "default fallback",
			["app:nothing"] = "scoped fallback",
			["other"] = "fallback",
			["app:other"] = "scoped fallback",
		};
		var evaluator = new TemplateEvaluator { Providers = { primary, fallback } };

		var result = evaluator.Evaluate("${NAME}/${APP:Name}/${nothing}/${app:nothing}/${other}/${app:other}");

		Assert.Equal("first/first scoped///fallback/scoped fallback", result);
	}

	[Fact]
	public void TemplateEvaluation_SeesDictionaryUpdates()
	{
		var variables = new Variables { ["name"] = "first", ["app:name"] = "scoped" };
		var evaluator = new TemplateEvaluator { Providers = { variables } };
		const string TEMPLATE = "${name}/${app:name}";

		Assert.Equal("first/scoped", evaluator.Evaluate(TEMPLATE));
		variables["NAME"] = "changed";
		variables["APP:NAME"] = null;
		Assert.Equal("changed/", evaluator.Evaluate(TEMPLATE));
		Assert.True(variables.Remove("app:name"));
		Assert.False(evaluator.TryEvaluate(TEMPLATE, out var result, out var error));
		Assert.Null(result);
		Assert.Equal("MissingVariable", error.Code);
		variables.Add("app:name", "restored");
		Assert.Equal("changed/restored", evaluator.Evaluate(TEMPLATE));
	}
}
