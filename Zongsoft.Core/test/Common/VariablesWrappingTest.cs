using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using Xunit;

using Zongsoft.Text.Templating;

namespace Zongsoft.Common.Tests;

public class VariablesWrappingTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void WrapAndExtension_RejectNullDictionary(bool reuse)
	{
		Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => Variables.Wrap((IDictionary<string, object>)null, reuse)).ParamName);
		Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => Variables.Wrap((IDictionary<object, object>)null, reuse)).ParamName);
		Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => Variables.Wrap((IDictionary)null, reuse)).ParamName);
		Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => ((IDictionary<string, object>)null).ToVariables(reuse)).ParamName);
		Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => ((IDictionary)null).ToVariables(reuse)).ParamName);
		Assert.Equal("dictionary", Assert.Throws<ArgumentNullException>(() => ((IDictionary<object, object>)null).ToVariables(reuse)).ParamName);
	}

	[Fact]
	public void WrapAndExtension_ReuseByReferenceIdentity()
	{
		var source = new EqualDictionary { ["name"] = "first" };
		var other = new EqualDictionary { ["name"] = "second" };
		var variables = Variables.Wrap(source, reuse: true);

		Assert.True(source.Equals(other));
		Assert.Same(variables, Variables.Wrap(source, reuse: true));
		Assert.Same(variables, source.ToVariables(reuse: true));
		Assert.NotSame(variables, other.ToVariables(reuse: true));
		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Equal("first", value);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void WrapAndExtension_ReturnExistingProvider(bool reuse)
	{
		IDictionary<string, object> source = new Variables { ["name"] = "value" };

		Assert.Same(source, Variables.Wrap(source, reuse));
		Assert.Same(source, source.ToVariables(reuse));
		Assert.Same(source, Variables.Wrap((IDictionary)source, reuse));
		Assert.Same(source, ((IDictionary)source).ToVariables(reuse));
	}

	[Fact]
	public void WrappingWithoutReuse_CreatesLiveViewsAndPreservesCachedView()
	{
		var source = new Dictionary<string, object> { ["Name"] = "initial" };
		var first = Variables.Wrap(source);
		var second = source.ToVariables();
		var cached = Variables.Wrap(source, reuse: true);
		var third = Variables.Wrap(source, reuse: false);
		var fourth = source.ToVariables(reuse: false);
		var views = new[] { first, second, cached, third, fourth };

		for(var index = 0; index < views.Length; index++)
		{
			for(var other = index + 1; other < views.Length; other++)
				Assert.NotSame(views[index], views[other]);
		}

		Assert.Same(cached, source.ToVariables(reuse: true));
		source["Name"] = null;

		Assert.All(views, variables =>
		{
			Assert.True(variables.TryGetValue("NAME", out var value));
			Assert.Null(value);
		});

		source.Remove("Name");
		Assert.All(views, variables => Assert.False(variables.TryGetValue("name", out _)));
	}

	[Fact]
	public void ConcurrentWrapping_ReturnsSameView()
	{
		var source = new Dictionary<string, object>();
		var results = new IVariables[128];

		Parallel.For(0, results.Length, index => results[index] = index % 2 == 0 ? Variables.Wrap(source, reuse: true) : source.ToVariables(reuse: true));

		Assert.All(results, result => Assert.Same(results[0], result));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void Lookup_UsesOrdinalIgnoreCaseAndNamespaceRules(int kind)
	{
		var source = CreateDictionary(kind);
		var raw = new object();
		source["Name"] = raw;
		source["App.Runtime:Name"] = "scoped";
		source["App:Nothing"] = null;
		source["Nothing"] = null;
		source[" app : name "] = "spaced";
		source[" :Name"] = "whitespace namespace";
		source[":Name"] = "colon key";
		source[""] = "empty name";
		var variables = kind == 4 ? new ReadOnlyDictionary<string, object>(source).ToVariables() : source.ToVariables();

		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Same(raw, value);
		Assert.True(variables.TryGetValue(null, "name", out value));
		Assert.Same(raw, value);
		Assert.True(variables.TryGetValue(string.Empty, "name", out value));
		Assert.Same(raw, value);
		Assert.True(variables.TryGetValue("APP.RUNTIME", "NAME", out value));
		Assert.Equal("scoped", value);
		Assert.True(variables.TryGetValue("nothing", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("APP", "NOTHING", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue(" app ", " name ", out value));
		Assert.Equal("spaced", value);
		Assert.True(variables.TryGetValue(" ", "NAME", out value));
		Assert.Equal("whitespace namespace", value);
		Assert.True(variables.TryGetValue(string.Empty, out value));
		Assert.Equal("empty name", value);
		Assert.False(variables.TryGetValue("APP", "name", out value));
		Assert.Null(value);
		Assert.False(variables.TryGetValue("runtime", "name", out _));
		Assert.False(variables.TryGetValue("missing", out value));
		Assert.Null(value);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue("app", null, out _)).ParamName);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	[InlineData(4)]
	[InlineData(5)]
	public void View_ReflectsAddReplaceRemoveAndClear(int kind)
	{
		var source = CreateDictionary(kind);
		var raw = new object();
		var variables = kind == 4 ? new ReadOnlyDictionary<string, object>(source).ToVariables() : source.ToVariables();

		Assert.False(variables.TryGetValue("name", out _));
		source.Add("Name", raw);
		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Same(raw, value);
		source["Name"] = null;
		Assert.True(variables.TryGetValue("NAME", out value));
		Assert.Null(value);
		Assert.True(source.Remove("Name"));
		Assert.False(variables.TryGetValue("name", out _));
		source.Add("App:Name", raw);
		Assert.True(variables.TryGetValue("app", "name", out value));
		Assert.Same(raw, value);
		source.Clear();
		Assert.False(variables.TryGetValue("app", "name", out _));
	}

	[Fact]
	public void Lookup_DoesNotUseIncompatibleSourceComparer()
	{
		var comparer = EqualityComparer<string>.Create(
			(left, right) => string.Equals(left.Replace("-", ""), right.Replace("-", ""), StringComparison.OrdinalIgnoreCase),
			value => StringComparer.OrdinalIgnoreCase.GetHashCode(value.Replace("-", "")));
		var source = new Dictionary<string, object>(comparer) { ["user-name"] = "value" };
		var variables = source.ToVariables();

		Assert.True(source.ContainsKey("USERNAME"));
		Assert.False(variables.TryGetValue("USERNAME", out _));
		Assert.True(variables.TryGetValue("USER-NAME", out var value));
		Assert.Equal("value", value);
	}

	[Theory]
	[InlineData(null, "Name")]
	[InlineData(null, "name")]
	[InlineData("App", "Name")]
	[InlineData("APP", "NAME")]
	public void Lookup_ReturnsFirstMatchingEntryIncludingNull(string scope, string name)
	{
		var source = new Dictionary<string, object>(StringComparer.Ordinal) { ["Name"] = null, ["App:Name"] = null };
		var variables = source.ToVariables();
		var first = scope == null ? "Name" : "App:Name";
		var second = scope == null ? "name" : "app:name";

		Assert.True(variables.TryGetValue(scope, name, out var value));
		Assert.Null(value);
		source.Add(second, "second");
		Assert.True(variables.TryGetValue(scope, name, out value));
		Assert.Null(value);

		if(scope == null)
		{
			Assert.True(variables.TryGetValue(name, out value));
			Assert.Null(value);
		}

		Assert.False(variables.TryGetValue("missing", out _));
		Assert.True(variables.TryGetValue(scope == null ? "app" : null, "name", out value));
		Assert.Null(value);
		source.Remove(first);
		Assert.True(variables.TryGetValue(scope, name, out value));
		Assert.Equal("second", value);
	}

	[Fact]
	public void TemplateEvaluation_SeesUpdatesAndNullValuesBlockFallback()
	{
		var source = new Dictionary<string, object> { ["Name"] = "first", ["App:Name"] = null };
		var fallback = new Variables { ["App:Name"] = "fallback" };
		var evaluator = new TemplateEvaluator { Providers = { source.ToVariables(), fallback } };
		const string TEMPLATE = "${NAME}/${APP:NAME}";

		Assert.Equal("first/", evaluator.Evaluate(TEMPLATE));
		source["Name"] = "updated";
		source.Remove("App:Name");
		Assert.Equal("updated/fallback", evaluator.Evaluate(TEMPLATE));
		source.Add("name", "second match");
		Assert.Equal("updated/fallback", evaluator.Evaluate(TEMPLATE));
	}

	[Fact]
	public void Cache_DoesNotKeepUnreferencedDictionaryOrViewAlive()
	{
		var references = CreateWeakReferences();

		Collect();

		Assert.False(references.Source.IsAlive);
		Assert.False(references.View.IsAlive);
	}

	[Fact]
	public void Cache_KeepsViewWhileDictionaryIsAlive()
	{
		var source = new Dictionary<string, object>();
		var reference = CreateWeakView(source);

		Collect();

		Assert.True(reference.IsAlive);
		Assert.Same(reference.Target, source.ToVariables(reuse: true));
		GC.KeepAlive(source);
	}

	[Fact]
	public void WrappingWithoutReuse_DoesNotKeepViewsWhileDictionaryIsAlive()
	{
		var source = new Dictionary<string, object>();
		var references = CreateUncachedWeakViews(source);

		Collect();

		Assert.False(references.Wrapped.IsAlive);
		Assert.False(references.Converted.IsAlive);
		GC.KeepAlive(source);
	}

	[Fact]
	public void View_KeepsSourceAvailableWhileInUse()
	{
		var variables = CreateView(out var reference);

		Collect();

		Assert.True(reference.IsAlive);
		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Equal("value", value);
		GC.KeepAlive(variables);
	}

	private static IDictionary<string, object> CreateDictionary(int kind) => kind switch
	{
		0 => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
		2 => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase),
		3 => new ConcurrentDictionary<string, object>(StringComparer.Ordinal),
		5 => new SortedDictionary<string, object>(StringComparer.OrdinalIgnoreCase),
		_ => new Dictionary<string, object>(StringComparer.Ordinal),
	};

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference Source, WeakReference View) CreateWeakReferences()
	{
		var source = new Dictionary<string, object>();
		return (new WeakReference(source), new WeakReference(source.ToVariables(reuse: true)));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateWeakView(IDictionary<string, object> source) => new(source.ToVariables(reuse: true));

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference Wrapped, WeakReference Converted) CreateUncachedWeakViews(IDictionary<string, object> source) =>
		(new WeakReference(Variables.Wrap(source, reuse: false)), new WeakReference(source.ToVariables(reuse: false)));

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IVariables CreateView(out WeakReference reference)
	{
		var source = new Dictionary<string, object> { ["Name"] = "value" };
		reference = new WeakReference(source);
		return source.ToVariables();
	}

	private static void Collect()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private sealed class EqualDictionary : Dictionary<string, object>
	{
		public override bool Equals(object obj) => obj is EqualDictionary;
		public override int GetHashCode() => 0;
	}
}
