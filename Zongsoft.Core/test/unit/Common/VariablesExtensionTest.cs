using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Common.Tests;

public class VariablesExtensionTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("app.worker")]
	[InlineData("default")]
	[InlineData("*")]
	public void Lookup_WithoutFallbackOnlyQueriesTheRequestedNamespace(string scope)
	{
		var calls = new List<(string Scope, bool Fallback)>();
		IVariables[] providers =
		[
			new Provider((ns, _, fallback) =>
			{
				calls.Add((string.IsNullOrEmpty(ns) ? null : ns, fallback));
				return (false, null);
			}),
			new Provider((ns, _, fallback) =>
			{
				calls.Add((string.IsNullOrEmpty(ns) ? null : ns, fallback));
				return (false, null);
			}),
		];

		Assert.False(providers.TryGetValue(scope, "missing", out var value));
		Assert.Null(value);
		Assert.Equal([(string.IsNullOrEmpty(scope) ? null : scope, false), (string.IsNullOrEmpty(scope) ? null : scope, false)], calls);
		calls.Clear();
		Assert.False(providers.TryGetValue(scope, "missing", false, out value));
		Assert.Null(value);
		Assert.Equal([(string.IsNullOrEmpty(scope) ? null : scope, false), (string.IsNullOrEmpty(scope) ? null : scope, false)], calls);
	}

	[Fact]
	public void Lookup_ParentNamespacePrecedesEarlierGlobalSources()
	{
		IVariables[] providers =
		[
			new Variables { ["name"] = "first global", ["app:name"] = "first parent" },
			new Variables { ["app.worker:name"] = "second nearer parent", ["app:name"] = "second parent" },
		];

		Assert.True(providers.TryGetValue("app.worker.child", "name", true, out var value));
		Assert.Equal("second nearer parent", value);
		Assert.True(providers.TryGetValue("app.other", "name", true, out value));
		Assert.Equal("first parent", value);
		Assert.True(providers.TryGetValue("other.child", "name", true, out value));
		Assert.Equal("first global", value);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void Lookup_OwnDefaultsFollowEveryNamespaceLayerAcrossAllSources(bool firstHasDefault)
	{
		var calls = new List<(int Source, string Scope, bool Fallback)>();
		IVariables[] providers =
		[
			new Provider((scope, _, fallback) =>
			{
				calls.Add((1, scope, fallback));
				return fallback && firstHasDefault ? (true, "first default") : (false, null);
			}),
			new Provider((scope, _, fallback) =>
			{
				calls.Add((2, scope, fallback));
				return fallback ? (true, "later default") : (false, null);
			}),
		];

		Assert.True(providers.TryGetValue("app.worker", "name", true, out var value));
		Assert.Equal(firstHasDefault ? "first default" : "later default", value);
		var expected = new List<(int Source, string Scope, bool Fallback)>
		{
			(1, "app.worker", false), (2, "app.worker", false),
			(1, "app", false), (2, "app", false),
			(1, null, false), (2, null, false),
			(1, null, true),
		};

		if(!firstHasDefault)
			expected.Add((2, null, true));

		Assert.Equal(expected, calls);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("app.worker")]
	public void Lookup_GlobalValuesPrecedeEarlierOwnDefaults(string scope)
	{
		var fallbackCalls = 0;
		IVariables[] providers =
		[
			new Provider((_, _, fallback) =>
			{
				if(fallback)
					fallbackCalls++;

				return fallback ? (true, "descriptor") : (false, null);
			}),
			new Variables { ["name"] = "configured" },
		];

		Assert.True(providers.TryGetValue(scope, "name", true, out var value));
		Assert.Equal("configured", value);
		Assert.Equal(0, fallbackCalls);
	}

	[Theory]
	[InlineData(0, null)]
	[InlineData(0, "")]
	[InlineData(0, false)]
	[InlineData(0, 0)]
	[InlineData(1, null)]
	[InlineData(1, "")]
	[InlineData(1, false)]
	[InlineData(1, 0)]
	[InlineData(2, null)]
	[InlineData(2, "")]
	[InlineData(2, false)]
	[InlineData(2, 0)]
	[InlineData(3, null)]
	[InlineData(3, "")]
	[InlineData(3, false)]
	[InlineData(3, 0)]
	public void Lookup_FoundValuesStopNamespaceAndSourceFallback(int layer, object expected)
	{
		var scope = layer switch { 0 => "app.worker", 1 => "app", _ => null };
		var calls = 0;
		IVariables[] providers =
		[
			new Provider((ns, _, fallback) =>
			{
				calls++;
				return ns == scope && fallback == (layer == 3) ? (true, expected) : (false, null);
			}),
			new Provider((ns, _, fallback) => ns == scope && fallback == (layer == 3) ? throw new InvalidOperationException("The match must stop lookup.") : (false, null)),
		];

		Assert.True(providers.TryGetValue("app.worker", "name", true, out var value));
		Assert.Equal(expected, value);
		Assert.Equal(layer + 1, calls);
	}

	[Fact]
	public void Lookup_StarAndDefaultAreOrdinaryNamespaceNames()
	{
		IVariables[] providers = [new Variables { ["name"] = "root", ["default:name"] = "default namespace", ["*:name"] = "star namespace" }];

		Assert.True(providers.TryGetValue("name", out var value));
		Assert.Equal("root", value);
		Assert.True(providers.TryGetValue("name", false, out value));
		Assert.Equal("root", value);
		Assert.True(providers.TryGetValue("DEFAULT", "name", out value));
		Assert.Equal("default namespace", value);
		Assert.True(providers.TryGetValue("*", "name", true, out value));
		Assert.Equal("star namespace", value);
		Assert.False(Array.Empty<IVariables>().TryGetValue("missing", true, out value));
		Assert.Null(value);
	}

	[Fact]
	public void Lookup_RejectsNullArgumentsAndSkipsNullProviders()
	{
		IEnumerable<IVariables> missing = null;
		IVariables[] providers = [new Variables()];

		Assert.Equal("variables", Assert.Throws<ArgumentNullException>(() => missing.TryGetValue("name", out _)).ParamName);
		Assert.Equal("variables", Assert.Throws<ArgumentNullException>(() => missing.TryGetValue("name", true, out _)).ParamName);
		Assert.Equal("variables", Assert.Throws<ArgumentNullException>(() => missing.TryGetValue("app", "name", out _)).ParamName);
		Assert.Equal("variables", Assert.Throws<ArgumentNullException>(() => missing.TryGetValue("app", "name", true, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => providers.TryGetValue(null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => providers.TryGetValue(null, true, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => providers.TryGetValue("app", null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => providers.TryGetValue("app", null, true, out _)).ParamName);
		IVariables[] sources =
		[
			null,
			new Variables { ["found"] = null },
			null,
			new Provider((scope, name, fallback) => (scope, name, fallback) switch
			{
				(null, "later", false) => (true, "configured"),
				(null, "fallback", true) => (true, "default"),
				_ => (false, null),
			}),
			null,
		];

		Assert.True(sources.TryGetValue("found", out var value));
		Assert.Null(value);
		Assert.True(sources.TryGetValue("later", out value));
		Assert.Equal("configured", value);
		Assert.True(sources.TryGetValue("app.worker", "fallback", true, out value));
		Assert.Equal("default", value);
		Assert.False(sources.TryGetValue("missing", true, out value));
		Assert.Null(value);
	}

	[Theory]
	[InlineData("app.worker", false)]
	[InlineData("app", false)]
	[InlineData(null, false)]
	[InlineData(null, true)]
	public void Lookup_PropagatesProviderFailuresFromEveryLookupStage(string failedScope, bool failedFallback)
	{
		var cause = new InvalidOperationException("Provider failure.");
		IVariables[] providers = [new Provider((scope, _, fallback) => scope == failedScope && fallback == failedFallback ? throw cause : (false, null))];

		Assert.Same(cause, Assert.Throws<InvalidOperationException>(() => providers.TryGetValue("app.worker", "name", true, out _)));
	}

	private sealed class Provider(Func<string, string, bool, (bool Found, object Value)> get) : IVariables
	{
		public bool TryGetValue(string name, out object value) => this.TryGetValue(null, name, false, out value);
		public bool TryGetValue(string name, bool fallback, out object value) => this.TryGetValue(null, name, fallback, out value);
		public bool TryGetValue(string @namespace, string name, out object value) => this.TryGetValue(@namespace, name, false, out value);
		public bool TryGetValue(string @namespace, string name, bool fallback, out object value)
		{
			var result = get(@namespace, name, fallback);
			value = result.Value;
			return result.Found;
		}
	}
}
