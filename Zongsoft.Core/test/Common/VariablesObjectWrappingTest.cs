using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;

using Xunit;

namespace Zongsoft.Common.Tests;

public class VariablesObjectWrappingTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void WrapAndExtension_AcceptConcreteAndInterfaceDictionaries(bool reuse)
	{
		var strings = new Dictionary<string, object> { ["Name"] = "string key" };
		var objects = new Dictionary<object, object> { ["Name"] = "object key" };
		var table = new Hashtable { ["Name"] = "non-generic key" };
		var genericStrings = new GenericDictionary<string> { ["Name"] = "generic-only string key" };
		var genericObjects = new GenericDictionary<object> { ["Name"] = "generic-only object key" };
		(IVariables Variables, string Expected)[] views =
		[
			(Variables.Wrap(strings, reuse), "string key"),
			(strings.ToVariables(reuse), "string key"),
			(Variables.Wrap((IDictionary<string, object>)strings, reuse), "string key"),
			(((IDictionary<string, object>)strings).ToVariables(reuse), "string key"),
			(Variables.Wrap((IDictionary)strings, reuse), "string key"),
			(((IDictionary)strings).ToVariables(reuse), "string key"),
			(Variables.Wrap(objects, reuse), "object key"),
			(objects.ToVariables(reuse), "object key"),
			(Variables.Wrap((IDictionary<object, object>)objects, reuse), "object key"),
			(((IDictionary<object, object>)objects).ToVariables(reuse), "object key"),
			(Variables.Wrap((IDictionary)objects, reuse), "object key"),
			(((IDictionary)objects).ToVariables(reuse), "object key"),
			(Variables.Wrap(table, reuse), "non-generic key"),
			(table.ToVariables(reuse), "non-generic key"),
			(Variables.Wrap((IDictionary)table, reuse), "non-generic key"),
			(((IDictionary)table).ToVariables(reuse), "non-generic key"),
			(Variables.Wrap(genericStrings, reuse), "generic-only string key"),
			(genericStrings.ToVariables(reuse), "generic-only string key"),
			(Variables.Wrap(genericObjects, reuse), "generic-only object key"),
			(genericObjects.ToVariables(reuse), "generic-only object key"),
		];

		foreach(var view in views)
		{
			Assert.True(view.Variables.TryGetValue("NAME", out var value));
			Assert.Equal(view.Expected, value);
		}
	}

	[Fact]
	public void WrapAndExtension_ReuseAcrossStaticTypes()
	{
		var strings = new Dictionary<string, object> { ["Name"] = "string key" };
		var objects = new Dictionary<object, object> { ["Name"] = "object key" };
		var stringView = Variables.Wrap(strings, reuse: true);
		var objectView = Variables.Wrap(objects, reuse: true);

		Assert.Same(stringView, Variables.Wrap((IDictionary<string, object>)strings, reuse: true));
		Assert.Same(stringView, Variables.Wrap((IDictionary)strings, reuse: true));
		Assert.Same(stringView, ((IDictionary<string, object>)strings).ToVariables(reuse: true));
		Assert.Same(stringView, ((IDictionary)strings).ToVariables(reuse: true));
		Assert.Same(stringView, strings.ToVariables(reuse: true));
		Assert.Same(objectView, Variables.Wrap((IDictionary<object, object>)objects, reuse: true));
		Assert.Same(objectView, Variables.Wrap((IDictionary)objects, reuse: true));
		Assert.Same(objectView, ((IDictionary<object, object>)objects).ToVariables(reuse: true));
		Assert.Same(objectView, ((IDictionary)objects).ToVariables(reuse: true));
		Assert.Same(objectView, objects.ToVariables(reuse: true));
		Assert.NotSame(stringView, objectView);
		Assert.True(stringView.TryGetValue("NAME", out var stringValue));
		Assert.Equal("string key", stringValue);
		Assert.True(objectView.TryGetValue("NAME", out var objectValue));
		Assert.Equal("object key", objectValue);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Lookup_MixedKeys_ReturnsFirstStringMatchIncludingNull(int kind)
	{
		var source = CreateDictionary(kind);
		Set(source, 42, "numeric key");
		Set(source, new NonStringKey(), "object key");
		Set(source, "Name", null);
		Set(source, "name", "second");
		var variables = WrapDictionary(source);

		Assert.False(variables.TryGetValue("42", out var value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("NAME", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue(null, "name", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue(string.Empty, "Name", out value));
		Assert.Null(value);
		Assert.False(variables.TryGetValue("app", "name", out value));
		Assert.Null(value);

		Remove(source, "Name");
		Assert.True(variables.TryGetValue("NAME", out value));
		Assert.Equal("second", value);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue(null, out _)).ParamName);
		Assert.Equal("name", Assert.Throws<ArgumentNullException>(() => variables.TryGetValue("app", null, out _)).ParamName);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Lookup_MixedKeys_UsesNamespaceRulesAndReflectsMutations(int kind)
	{
		var source = CreateDictionary(kind);
		var raw = new object();
		var variables = ConvertDictionary(source);

		Assert.False(variables.TryGetValue("name", out _));
		Set(source, "Name", raw);
		Set(source, "App.Runtime:Name", "scoped");
		Set(source, " app : name ", "spaced");
		Set(source, " :Name", "whitespace namespace");
		Set(source, string.Empty, "empty name");
		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Same(raw, value);
		Assert.True(variables.TryGetValue("APP.RUNTIME", "NAME", out value));
		Assert.Equal("scoped", value);
		Assert.True(variables.TryGetValue("app.runtime:name", out value));
		Assert.Equal("scoped", value);
		Assert.True(variables.TryGetValue(" app ", " name ", out value));
		Assert.Equal("spaced", value);
		Assert.True(variables.TryGetValue(" ", "NAME", out value));
		Assert.Equal("whitespace namespace", value);
		Assert.True(variables.TryGetValue(string.Empty, out value));
		Assert.Equal("empty name", value);
		Assert.False(variables.TryGetValue(" name ", out value));
		Assert.Null(value);
		Assert.False(variables.TryGetValue("app", "name", out value));
		Assert.Null(value);

		Set(source, "App.Runtime:Name", null);
		Assert.True(variables.TryGetValue("app.runtime", "name", out value));
		Assert.Null(value);
		Remove(source, "App.Runtime:Name");
		Assert.False(variables.TryGetValue("app.runtime", "name", out value));
		Assert.Null(value);
		Assert.True(variables.TryGetValue("name", out value));
		Assert.Same(raw, value);
		Clear(source);
		Assert.False(variables.TryGetValue("name", out value));
		Assert.Null(value);
	}

	[Fact]
	public void Wrap_PrefersGenericDictionaryInterfaces()
	{
		var strings = new StringPreferredDictionary { ["Name"] = "string interface" };
		var objects = new ObjectPreferredDictionary { ["Name"] = "object interface" };
		var variables = Variables.Wrap(strings, reuse: true);

		Assert.True(Variables.Wrap((IDictionary<object, object>)strings).TryGetValue("NAME", out var value));
		Assert.Equal("string interface", value);
		Assert.Same(variables, Variables.Wrap((IDictionary<string, object>)strings, reuse: true));
		Assert.Same(variables, Variables.Wrap((IDictionary<object, object>)strings, reuse: true));
		Assert.Same(variables, ((IDictionary<object, object>)strings).ToVariables(reuse: true));
		Assert.Same(variables, ((IDictionary)strings).ToVariables(reuse: true));
		Assert.True(variables.TryGetValue("NAME", out value));
		Assert.Equal("string interface", value);
		Assert.True(((IDictionary)objects).ToVariables().TryGetValue("NAME", out value));
		Assert.Equal("object interface", value);
	}

	[Fact]
	public void Wrap_NonGenericDictionaryCanExposeOtherValueTypes()
	{
		var source = new Dictionary<string, int> { ["Count"] = 42 };
		var variables = source.ToVariables();

		Assert.True(variables.TryGetValue("COUNT", out var value));
		Assert.Equal(42, Assert.IsType<int>(value));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Cache_MixedKeyDictionaryAndViewCanBeCollected(int kind)
	{
		var references = CreateWeakReferences(kind);

		Collect();

		Assert.False(references.Source.IsAlive);
		Assert.False(references.View.IsAlive);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void Cache_LiveMixedKeySourceRetainsOnlyReusedView(int kind)
	{
		var source = CreateDictionary(kind);
		var references = CreateWeakViews(source);

		Collect();

		Assert.True(references.Cached.IsAlive);
		Assert.Same(references.Cached.Target, WrapDictionary(source, reuse: true));
		Assert.False(references.Wrapped.IsAlive);
		Assert.False(references.Converted.IsAlive);
		GC.KeepAlive(source);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public void View_KeepsMixedKeySourceAvailableWhileInUse(int kind)
	{
		var variables = CreateView(kind, out var reference);

		Collect();

		Assert.True(reference.IsAlive);
		Assert.True(variables.TryGetValue("NAME", out var value));
		Assert.Equal("value", value);
		GC.KeepAlive(variables);
	}

	private static object CreateDictionary(int kind) => kind switch
	{
		0 => new OrderedDictionary(StringComparer.Ordinal),
		1 => new GenericDictionary<object>(),
		_ => new Dictionary<object, object>(),
	};

	private static IVariables WrapDictionary(object source, bool reuse = false) => source is IDictionary<object, object> dictionary ?
		Variables.Wrap(dictionary, reuse) : Variables.Wrap((IDictionary)source, reuse);

	private static IVariables ConvertDictionary(object source, bool reuse = false) => source is IDictionary<object, object> dictionary ?
		dictionary.ToVariables(reuse) : ((IDictionary)source).ToVariables(reuse);

	private static void Set(object source, object key, object value)
	{
		if(source is IDictionary<object, object> dictionary)
			dictionary[key] = value;
		else
			((IDictionary)source)[key] = value;
	}

	private static void Remove(object source, object key)
	{
		if(source is IDictionary<object, object> dictionary)
			dictionary.Remove(key);
		else
			((IDictionary)source).Remove(key);
	}

	private static void Clear(object source)
	{
		if(source is IDictionary<object, object> dictionary)
			dictionary.Clear();
		else
			((IDictionary)source).Clear();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference Source, WeakReference View) CreateWeakReferences(int kind)
	{
		var source = CreateDictionary(kind);
		return (new WeakReference(source), new WeakReference(ConvertDictionary(source, reuse: true)));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (WeakReference Cached, WeakReference Wrapped, WeakReference Converted) CreateWeakViews(object source) =>
		(new WeakReference(ConvertDictionary(source, reuse: true)), new WeakReference(WrapDictionary(source)), new WeakReference(ConvertDictionary(source)));

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static IVariables CreateView(int kind, out WeakReference reference)
	{
		var source = CreateDictionary(kind);
		Set(source, "Name", "value");
		reference = new WeakReference(source);
		return WrapDictionary(source);
	}

	private static void Collect()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	private sealed class NonStringKey
	{
		public override string ToString() => throw new InvalidOperationException("Non-string keys must not be converted.");
	}

	private sealed class GenericDictionary<TKey> : IDictionary<TKey, object>
	{
		private readonly Dictionary<TKey, object> _entries = new();
		public object this[TKey key] { get => _entries[key]; set => _entries[key] = value; }
		public ICollection<TKey> Keys => _entries.Keys;
		public ICollection<object> Values => _entries.Values;
		public int Count => _entries.Count;
		public bool IsReadOnly => false;
		public void Add(TKey key, object value) => _entries.Add(key, value);
		public void Add(KeyValuePair<TKey, object> item) => ((ICollection<KeyValuePair<TKey, object>>)_entries).Add(item);
		public void Clear() => _entries.Clear();
		public bool Contains(KeyValuePair<TKey, object> item) => ((ICollection<KeyValuePair<TKey, object>>)_entries).Contains(item);
		public bool ContainsKey(TKey key) => _entries.ContainsKey(key);
		public void CopyTo(KeyValuePair<TKey, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<TKey, object>>)_entries).CopyTo(array, arrayIndex);
		public bool Remove(TKey key) => _entries.Remove(key);
		public bool Remove(KeyValuePair<TKey, object> item) => ((ICollection<KeyValuePair<TKey, object>>)_entries).Remove(item);
		public bool TryGetValue(TKey key, out object value) => _entries.TryGetValue(key, out value);
		public IEnumerator<KeyValuePair<TKey, object>> GetEnumerator() => _entries.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	private sealed class ObjectPreferredDictionary : Dictionary<object, object>, IDictionary
	{
		IDictionaryEnumerator IDictionary.GetEnumerator() => throw new InvalidOperationException("The generic interface must be preferred.");
	}

	private sealed class StringPreferredDictionary : Dictionary<string, object>, IDictionary<object, object>, IDictionary
	{
		object IDictionary<object, object>.this[object key] { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		ICollection<object> IDictionary<object, object>.Keys => throw new NotSupportedException();
		ICollection<object> IDictionary<object, object>.Values => throw new NotSupportedException();
		bool ICollection<KeyValuePair<object, object>>.IsReadOnly => false;
		void IDictionary<object, object>.Add(object key, object value) => throw new NotSupportedException();
		bool IDictionary<object, object>.ContainsKey(object key) => throw new NotSupportedException();
		bool IDictionary<object, object>.Remove(object key) => throw new NotSupportedException();
		bool IDictionary<object, object>.TryGetValue(object key, out object value) => throw new NotSupportedException();
		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> item) => throw new NotSupportedException();
		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> item) => throw new NotSupportedException();
		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) => throw new NotSupportedException();
		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> item) => throw new NotSupportedException();
		IEnumerator<KeyValuePair<object, object>> IEnumerable<KeyValuePair<object, object>>.GetEnumerator() => throw new InvalidOperationException("The string-key interface must be preferred.");
		IDictionaryEnumerator IDictionary.GetEnumerator() => throw new InvalidOperationException("The generic interface must be preferred.");
	}
}
