using System;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class IdentifierTest
{
	[Fact]
	public void TestGeneric()
	{
		var identifier = new Identifier<uint>("IdentifierTest", 100u);
		Assert.True(identifier.HasValue);
		Assert.False(identifier.IsEmpty);
		Assert.Equal("IdentifierTest", identifier.Name);
		Assert.Null(identifier.Label);
		Assert.Null(identifier.Description);
		Assert.Equal(100u, identifier.Value);
		Assert.Equal(100u, (uint)identifier);

		Assert.True(identifier.TryGetValue(out var value));
		Assert.Equal(100U, value);
		Assert.True(identifier.TryGetValue("IdentifierTest", out value));
		Assert.Equal(100U, value);
		Assert.False(identifier.TryGetValue("Exception", out value));
	}

	[Fact]
	public void TestGeneral()
	{
		var identifier = new Identifier("IdentifierTest", 100U);
		Assert.True(identifier.HasValue);
		Assert.False(identifier.IsEmpty);
		Assert.Equal("IdentifierTest", identifier.Name);
		Assert.Null(identifier.Label);
		Assert.Null(identifier.Description);
		Assert.Equal(100u, identifier.Value);
		Assert.Equal(100u, (uint)identifier);

		Assert.True(identifier.TryGetValue(out uint value));
		Assert.Equal(100U, value);
		Assert.True(identifier.TryGetValue<uint>("IdentifierTest", out value));
		Assert.Equal(100U, value);
		Assert.False(identifier.TryGetValue<uint>("Exception", out value));
	}

	[Fact]
	public void TestConvertible()
	{
		var identifier = new Identifier("IdentifierTest", 100);
		Assert.Equal(100, (int)identifier);
		Assert.Equal(100U, (uint)identifier);
		Assert.Equal(100, (byte)identifier);
		Assert.Equal(100, (sbyte)identifier);
		Assert.Equal(100, (short)identifier);
		Assert.Equal(100, (ushort)identifier);
		Assert.Equal(100L, (long)identifier);
		Assert.Equal(100UL, (ulong)identifier);
		Assert.Equal(100f, (float)identifier);
		Assert.Equal(100d, (double)identifier);
		Assert.Equal(100m, (decimal)identifier);
		Assert.Equal("100", (string)identifier);
	}

	[Fact]
	public void TestDeserialize()
	{
		const string JSON = """
		{
			"Id" : "Person:<int>100",
			"Name" : "Popeye"
		}
		""";

		var result = System.Text.Json.JsonSerializer.Deserialize<Person>(JSON);
		Assert.NotNull(result.Id.Value);
		Assert.IsType<int>(result.Id.Value);
		Assert.Equal(100, (int)result.Id.Value);
		Assert.Equal("Popeye", result.Name);
	}

	[Fact]
	public void TestNameAndTypedEquality()
	{
		Assert.Equal(new Identifier("Machine", 17u, "First"), new Identifier("machine", 17u, "Second"));
		Assert.NotEqual(new Identifier("Machine", 17u), new Identifier("Machine", 17));
		Assert.NotEqual(new Identifier("Machine", 17u), new Identifier("Machine", "17"));
		Assert.NotEqual(new Identifier("Machine", "ABC"), new Identifier("Machine", "abc"));
		Assert.NotEqual(new Identifier("Machine", 17u), new Identifier("Person", 17u));
		Assert.Equal(new Identifier("Machine", 17u).GetHashCode(), new Identifier("machine", 17u).GetHashCode());
		Assert.Equal(default, Identifier.Parse(string.Empty));
		Assert.Equal(0, default(Identifier).GetHashCode());
		Assert.Throws<ArgumentNullException>(() => Identifier.Parse((string)null));
		Assert.False(Identifier.TryParse((string)null, out _));
	}

	[Fact]
	public void TestEmptyValidation()
	{
		var named = new Identifier("Example", null);
		var typed = new Identifier("IDisposable", null);
		Assert.True(named.IsEmpty);
		Assert.True(typed.IsEmpty);
		Assert.False(named.Validate("Example"));
		Assert.False(typed.Validate("IDisposable"));
		Assert.False(typed.TryGetValue<uint>("IDisposable", out _));
		Assert.False(typed.TryGetValue("IDisposable", out uint _));
		Assert.False(default(Identifier).Validate("IDisposable"));
		Assert.True(new Identifier("Exception", 0).Validate("Exception"));

		var calls = 0;
		var generic = new Identifier<string>("IDisposable", null);
		Assert.False(generic.Validate("IDisposable", _ => { calls++; return true; }));
		Assert.Equal(0, calls);
	}

	[Fact]
	public void TestAssignableScalarKeys()
	{
		Roundtrip(new Identifier<object>("Scalar", 17u));
		Roundtrip(new Identifier<object>("Scalar", "17"));
		Roundtrip(new Identifier<IComparable>("Scalar", 17u));
		Roundtrip(new Identifier<IComparable>("Scalar", "17"));
		Roundtrip(new Identifier<int?>("Scalar", 17));

		var identifier = Identifier<object>.Parse("Scalar:<uint>17");
		Assert.IsType<uint>(identifier.Value);
		Assert.Equal(new Identifier("Scalar", 17u), (Identifier)identifier);
		Assert.Equal("Scalar:<uint>17", identifier.ToString());
		Assert.IsType<string>(Identifier<object>.Parse("Scalar:17").Value);
		Assert.True(Identifier<uint>.TryParse("Scalar:17", out var inferred));
		Assert.Equal(17u, inferred.Value);

		Identifier.Formatting.Register<uint>("Test.Scalar");
		Roundtrip(new Identifier<object>("Test.Scalar", 17u));
		Roundtrip(new Identifier<object>("Test.Scalar", "17"));
		Assert.IsType<uint>(Identifier<object>.Parse("Test.Scalar:17").Value);
	}

	[Fact]
	public void TestParsingPreservesKeyTypes()
	{
		Assert.False(Identifier<int>.TryParse("Scalar:<uint>17", out _));
		Assert.False(Identifier<uint>.TryParse("Scalar:<int>17", out _));
		Assert.False(Identifier<string>.TryParse("Scalar:<uint>17", out _));
		Assert.False(Identifier<uint>.TryParse("Scalar:<string>17", out _));
		Assert.False(Identifier<uint, uint>.TryParse("Pair:<uint,ushort>1,2", out _));
		Assert.False(Identifier<object, uint>.TryParse("Pair:<uint,uint>1,2", out _));
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Identifier<int>>("\"Scalar:<uint>17\""));
	}

	[Fact]
	public void TestFormattingRegistration()
	{
		Identifier.Formatting.Register<uint, uint>("Test.Branch");
		Identifier.Formatting.Register<uint, uint>("test.branch", '-');
		Assert.Throws<InvalidOperationException>(() => Identifier.Formatting.Register<uint, ushort>("Test.Branch", '-'));
		Assert.Throws<InvalidOperationException>(() => Identifier.Formatting.Register<uint, uint>("Test.Branch", ','));
		var identifier = new Identifier<uint, uint>("Test.Branch", 1, 1001);
		Assert.Equal("Test.Branch:1-1001", identifier.ToString());
		Assert.Equal("Test.Branch:<uint,uint>1,1001", identifier.ToString("F"));
		Assert.Equal((Identifier)identifier, Identifier.Parse(identifier.ToString()));
		Assert.Equal(identifier, Identifier<uint, uint>.Parse(identifier.ToString()));
		Assert.False(Identifier.TryParse("Test.Branch:1-abc", out _));
		Assert.False(Identifier.TryParse("Test.Branch:1-2-3", out _));
		Assert.False(Identifier.TryParse("Test.Branch:1-4294967296", out _));
		Assert.Equal("Test.Branch:<string>1-1001", new Identifier("Test.Branch", "1-1001").ToString());
		Assert.IsType<string>(Identifier.Parse("Unregistered:1-1001").Value);
		Assert.Equal((1u, 2u), Identifier.Parse("Test.Branch:<uint,uint>1,2").Value);
		Assert.False(Identifier<uint, ushort>.TryParse(identifier.ToString(), out _));
	}

	[Fact]
	public void TestFormattingSeparators()
	{
		Identifier.Formatting.Register<string, int>("IdentifierTest");
		Identifier.Formatting.Register<string, int>("IdentifierTest", '-');
		var identifier = new Identifier<string, int>("IdentifierTest", "A-B", -2);
		Assert.EndsWith(@":A\-B-\-2", identifier.ToString());
		Roundtrip(identifier);

		Identifier.Formatting.Register<string, int>("Test.Comma", ',');
		var comma = new Identifier<string, int>("Test.Comma", "A,B", -2);
		Assert.Equal(@"Test.Comma:A\,B,-2", comma.ToString());
		Roundtrip(comma);
		Assert.Equal(comma, Identifier<string, int>.Parse(comma.ToString("F")));
	}

	[Theory]
	[InlineData(32)]
	[InlineData(512)]
	public void TestEscapedTokenSlices(int length)
	{
		var text = new string('x', length) + "\\?#()|\r\n\t\u0001中文";
		var properties = new Dictionary<string, string> { [text] = text, [""] = "", ["Plain"] = "Value" };
		var identifier = new Identifier("Slices", text, (text, text, text), text, properties);
		var parsed = Identifier.Parse(identifier.ToString());
		Assert.Equal(text, parsed.Value);
		Assert.Equal(text, parsed.Code);
		Assert.Equal(text, parsed.Label);
		Assert.Equal(text, parsed.Alias);
		Assert.Equal(text, parsed.Description);
		Assert.Equal(text, parsed.Properties[text]);
		Assert.Equal("", parsed.Properties[""]);
		Assert.Equal("Value", parsed.Properties["Plain"]);
		Roundtrip(identifier);
		Roundtrip(new Identifier<string, string, string, string, string>("Slices", "", text, "", "中文", "\\"));
	}

	[Fact]
	public void TestMetadataAndEscaping()
	{
		var properties = new Dictionary<string, string> { ["Region"] = "East;West={x}\\", ["Empty"] = "" };
		var identifier = new Identifier("Test", "a:b,?()|#{}\\\r\n\t\u0001", (null, "杭运"), "杭州|中心", "Description#{}", properties);
		var parsed = Identifier.Parse(identifier.ToString());
		Assert.Equal(identifier, parsed);
		Assert.Equal(identifier.Label, parsed.Label);
		Assert.Equal(identifier.Alias, parsed.Alias);
		Assert.Equal(identifier.Description, parsed.Description);
		Assert.Equal(properties["Region"], parsed.Properties["region"]);
		properties["Region"] = "Changed";
		Assert.NotEqual(properties["Region"], identifier.Properties["Region"]);
		var copy = identifier;
		copy.Properties["Region"] = "Shared";
		Assert.Equal("Shared", identifier.Properties["Region"]);
		Assert.Equal(identifier.GetHashCode(), parsed.GetHashCode());
		Assert.Equal("Alias:1?|A", new Identifier("Alias", "1", (null, "A")).ToString());
		Assert.Equal("Description:1(D)", new Identifier("Description", "1", null, "D").ToString());
	}

	[Fact]
	public void TestCodeMetadata()
	{
		var properties = new Dictionary<string, string> { ["Code"] = "Independent property" };
		var identifier = new Identifier("Order", 1001UL, (" SO#001?()<>|{}\\\r\n ", "PO", "Order"), "Internal (purchase)", properties);
		var parsed = Identifier.Parse(identifier.ToString());
		Assert.Equal(identifier.Code, parsed.Code);
		Assert.Equal(identifier.Label, parsed.Label);
		Assert.Equal(identifier.Alias, parsed.Alias);
		Assert.Equal(identifier.Description, parsed.Description);
		Assert.Equal("Independent property", parsed.Properties["Code"]);
		var other = new Identifier("Order", 1001UL, ("SO002", null));
		Assert.Equal(identifier, other);
		Assert.Equal(identifier.GetHashCode(), other.GetHashCode());
		Assert.True(identifier.Validate("Order"));
		Roundtrip(identifier);

		Assert.Equal("Order:<ulong>1001#SO001", new Identifier("Order", 1001UL, ("SO001", null)).ToString());
		Assert.Equal("Order:<ulong>1001", new Identifier("Order", 1001UL, ("", null)).ToString());
		var empty = new Identifier("Order", null, ("SO001", null));
		Assert.True(empty.IsEmpty);
		Assert.Equal("null", JsonSerializer.Serialize(empty));
	}

	[Fact]
	public void TestCodeConversions()
	{
		var source = new Identifier<uint, ushort>("Store", 1, 2, ("S002", null), "Store", null, new Dictionary<string, string>());
		Identifier untyped = source;
		Identifier<(uint, ushort)> tuple = source;
		Identifier<uint, ushort> restored = tuple;
		Assert.Equal("S002", untyped.Code);
		Assert.Equal("S002", tuple.Code);
		Assert.Equal("S002", restored.Code);
		Assert.Same(source.Properties, restored.Properties);
		Assert.Equal("S002", ((Identifier<uint, ushort>)untyped).Code);
		Roundtrip(source);
		Roundtrip(tuple);
		Roundtrip(new Identifier<uint>("Person", 1, ("P001", null)));
		Roundtrip(new Identifier<int, string, Guid>("Three", 1, "two", Guid.Empty, ("003", null)));
		Roundtrip(new Identifier<int, string, bool, decimal>("Four", 1, "two", true, 4m, ("004", null)));
		Roundtrip(new Identifier<int, string, bool, decimal, DateOnly>("Five", 1, "two", true, 4m, new(2026, 10, 31), ("005", null)));

		var model = new IdentifiedModel<uint>();
		IIdentifiable identifiable = model;
		identifiable.Identifier = new Identifier("Person", 1u, ("P001", null));
		Assert.Equal("P001", model.Identifier.Code);
		Assert.Equal("P001", identifiable.Identifier.Code);
	}

	[Theory]
	[InlineData("Example:", null, null, null, null)]
	[InlineData("Example:1#C", "C", null, null, null)]
	[InlineData("Example:1?L", null, "L", null, null)]
	[InlineData("Example:1?|A", null, "", "A", null)]
	[InlineData("Example:1(D)", null, null, null, "D")]
	[InlineData("Example:1#C(D)", "C", null, null, "D")]
	[InlineData("Example:1#C?L|A(D){x=y}", "C", "L", "A", "D")]
	public void TestOptionalMetadata(string text, string code, string label, string alias, string description)
	{
		var identifier = Identifier.Parse(text);
		Assert.Equal(code, identifier.Code);
		Assert.Equal(label, identifier.Label);
		Assert.Equal(alias, identifier.Alias);
		Assert.Equal(description, identifier.Description);
		Assert.Equal(text, identifier.ToString());
		Roundtrip(identifier);
	}

	[Fact]
	public void TestGenericScopeAndEscapedValues()
	{
		Roundtrip(new Identifier("Dictionary", 17u, ("D017", null), null, "(nested)"));
		Roundtrip(new Identifier("Text", "<uint>17#Code?(description){x=y}|alias", ("#?()<>", null)));
		Assert.False(Identifier.TryParse("$List<int>:17", out _));
		Assert.False(Identifier.TryParse("$List<int>:<uint>17", out _));
		Assert.Equal(string.Empty, Identifier.Parse("Text:<string>").Value);
	}

	[Fact]
	public void TestExtensionPropertyNames()
	{
		var properties = new Dictionary<string, string>
		{
			["Name"] = "Extension name",
			["中文-说明"] = "任意扩展名称",
			["space key"] = "value",
			["a;b={c}\\\r\n"] = "escaped",
			[""] = "empty key",
			["Null"] = null,
		};
		var identifier = new Identifier("Test", 17u, "Label", null, properties);
		identifier.Properties["Label"] = "Extension label";
		var text = identifier.ToString();
		var parsed = Identifier.Parse(text);
		Assert.Equal(identifier.Properties.Count, parsed.Properties.Count);
		Assert.Equal("Test", parsed.Name);
		Assert.Equal("Label", parsed.Label);

		foreach(var entry in identifier.Properties)
			Assert.Equal(entry.Value ?? string.Empty, parsed.Properties[entry.Key]);

		Assert.Equal(text, TypeDescriptor.GetConverter(typeof(Identifier)).ConvertToInvariantString(identifier));
		Assert.Equal(text, JsonSerializer.Deserialize<Identifier>(JsonSerializer.Serialize(identifier)).ToString());
	}

	[Theory]
	[InlineData(".Invalid:1")]
	[InlineData("中文:1")]
	[InlineData("$Missing.Type:1")]
	[InlineData("A:<uint,uint>1")]
	[InlineData("A:<uint>1,2")]
	[InlineData("A:<uint>4294967296")]
	[InlineData("A:<UnknownType>1")]
	[InlineData("A:1\\")]
	[InlineData("A:1\\q")]
	[InlineData("A:1\\u12")]
	[InlineData("A:1\\uZZZZ")]
	[InlineData("A:1\\\n")]
	[InlineData("A:1{x=y")]
	[InlineData("A:1{x=y;X=z}")]
	[InlineData("A:1{x=y;}")]
	[InlineData("A:1{x=y}!")]
	[InlineData("A:1}")]
	[InlineData("A:1?label?again")]
	[InlineData("A:1\n")]
	[InlineData("A:1?label\t")]
	[InlineData("A:<int")]
	[InlineData("A:<>")]
	[InlineData("A:<uint,>1")]
	[InlineData("A:<uint>>1")]
	[InlineData("A:<List<int>:1")]
	[InlineData("A:<int,int,int,int,int,int,int,int>1,2,3,4,5,6,7,8")]
	[InlineData("A:<int,int,int,int,int,int,int>1,2,3,4,5,6")]
	[InlineData("A(uint):1")]
	[InlineData("A:1#C#D")]
	[InlineData("A:1?L#C")]
	[InlineData("A:1|A")]
	[InlineData("A:1?L|A|B")]
	[InlineData("A:1(unclosed")]
	[InlineData("A:1(nested(raw))")]
	[InlineData("A:1(D)#C")]
	[InlineData("A:1(D)?L")]
	[InlineData("A:1(D)!")]
	[InlineData("A:1(D){x=y")]
	[InlineData("A:1{a=b}(D)")]
	[InlineData("A:1>")]
	[InlineData("$NoSuchType:<int>1")]
	public void TestInvalidState(string text)
	{
		Assert.False(Identifier.TryParse(text, out var result));
		Assert.Equal(default, result);
		Assert.Throws<FormatException>(() => Identifier.Parse(text));
	}

	[Fact]
	public void TestAllAritiesAndConverters()
	{
		Roundtrip(new Identifier<uint>("One", 1));
		Roundtrip(new Identifier<uint, ushort>("Two", 1, 2));
		Roundtrip(new Identifier<int, string, Guid>("Three", -1, "a,b", Guid.NewGuid()));
		Roundtrip(new Identifier<int, string, bool, decimal>("Four", -1, "a,b", true, 12.34m));
		Roundtrip(new Identifier<int, string, bool, decimal, DateOnly>("Five", -1, "a,b", true, 12.34m, new(2026, 10, 30)));
		var source = new Identifier<uint, ushort>("Store", 1, 2, (null, "Alias"), "Name", null, new Dictionary<string, string>());
		Identifier<(uint, ushort)> tuple = source;
		Identifier<uint, ushort> restored = tuple;
		Assert.Equal(source, restored);
		Assert.Equal(tuple, Identifier<(uint, ushort)>.Parse(source.ToString("F")));
		Assert.True(new Identifier("Store", (1, 2)).TryGetValue<(uint, ushort)>(out var converted));
		Assert.Equal((1u, (ushort)2), converted);
		Assert.Same(source.Properties, restored.Properties);
		Assert.Equal((Identifier)source, (Identifier)tuple);
		Assert.False(Identifier<int>.TryParse("One:<uint>1", out _));
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Identifier>("17"));
		Assert.Equal("null", JsonSerializer.Serialize(default(Identifier)));
		Assert.Equal(default, JsonSerializer.Deserialize<Identifier>("null"));
		var typed = new Identifier("Dictionary", 17u);
		Assert.Equal(typed, Identifier.Parse(typed.ToString()));
	}

	[Fact]
	public void TestSixAndSevenComponentKeys()
	{
		var date = new DateTime(2026, 11, 7);
		var six = new Identifier<(uint, uint, string, string, string, string)>("Statistic", (11u, 23u, "domain", "name", "tag", "key,one"));
		var seven = new Identifier<(uint, uint, string, string, string, string, DateTime)>("Statistic", (11u, 23u, "domain", "name", "tag", "key,one", date));
		Roundtrip(six);
		Roundtrip((Identifier)six);
		Roundtrip(seven);
		Identifier source = seven;
		Roundtrip(source);
		Assert.True(source.TryGetValue(6, out DateTime actual));
		Assert.Equal(date, actual);
		var changed = source.With(6, date.AddDays(1));
		Assert.NotEqual(source, changed);
		Assert.Equal((11u, 23u, "domain", "name", "tag", "key,one", date.AddDays(1)), changed.Value);
		Roundtrip(changed);
	}

	[Fact]
	public void TestSixAndSevenGenericContracts()
	{
		var properties = new Dictionary<string, string> { ["zone"] = "east" };
		var six = new Identifier<int, string, bool, decimal, long, byte>("Generic.Six", 1, "two", true, 4m, 5L, 6, ("C", "A"), "L", "D", properties);
		var seven = new Identifier<int, string, bool, decimal, long, byte, short>("Generic.Seven", 1, "two", true, 4m, 5L, 6, 7, ("C", "A"), "L", "D", properties);
		Roundtrip(six);
		Roundtrip(seven);
		Identifier<(int, string, bool, decimal, long, byte)> sixTuple = six;
		Identifier<(int, string, bool, decimal, long, byte, short)> sevenTuple = seven;
		Identifier<int, string, bool, decimal, long, byte> restoredSix = sixTuple;
		Identifier<int, string, bool, decimal, long, byte, short> restoredSeven = sevenTuple;
		Assert.Equal(six, restoredSix);
		Assert.Equal(seven, restoredSeven);
		Assert.Same(six.Properties, sixTuple.Properties);
		Assert.Same(seven.Properties, restoredSeven.Properties);
		Assert.NotSame(properties, seven.Properties);
		Assert.Equal("C", restoredSeven.Code);
		Assert.Equal("A", restoredSeven.Alias);
		Assert.Equal("L", restoredSeven.Label);
		Assert.Equal("D", restoredSeven.Description);

		var (a, b, c, d, e, f) = six;
		Assert.Equal(six.Value, (a, b, c, d, e, f));
		var (g, h, i, j, k, l, m) = seven;
		Assert.Equal(seven.Value, (g, h, i, j, k, l, m));
		Assert.True(six.TryGetValue(5, out byte sixth));
		Assert.True(seven.TryGetValue(6, out short seventh));
		Assert.Equal((byte)6, sixth);
		Assert.Equal((short)7, seventh);
		Assert.Equal((1, "two", true, 4m, 5L, (byte)9), six.With(5, "9").Value);
		Assert.Equal((1, "two", true, 4m, 5L, (byte)6, (short)9), seven.With(6, 9).Value);
		Assert.Throws<ArgumentOutOfRangeException>(() => six.With(6, 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => seven.With(7, 1));
		Assert.Throws<InvalidCastException>(() => six.With(5, 256));

		var sixCalls = 0;
		Func<int, string, bool, decimal, long, byte, bool> sixPredicate = (v1, v2, v3, v4, v5, v6) =>
		{
			sixCalls++;
			return v1 == 1 && v2 == "two" && v3 && v4 == 4m && v5 == 5L && v6 == 6;
		};
		var sevenCalls = 0;
		Func<int, string, bool, decimal, long, byte, short, bool> sevenPredicate = (v1, v2, v3, v4, v5, v6, v7) =>
		{
			sevenCalls++;
			return v1 == 1 && v2 == "two" && v3 && v4 == 4m && v5 == 5L && v6 == 6 && v7 == 7;
		};
		Assert.True(six.Validate("Generic.Six", sixPredicate));
		Assert.True(seven.Validate("Generic.Seven", sevenPredicate));
		Assert.False(six.Validate("Missing", sixPredicate));
		Assert.False(seven.Validate("IDisposable", sevenPredicate));
		Assert.Equal(1, sixCalls);
		Assert.Equal(1, sevenCalls);
		Assert.Throws<ArgumentNullException>(() => six.Validate("Generic.Six", null));
		Assert.Throws<ArgumentNullException>(() => seven.Validate("Person", null));

		var updatedSix = six with { Value = (2, "other", false, 8m, 9L, 10), Label = "Updated" };
		var updatedSeven = seven with { Value = (2, "other", false, 8m, 9L, 10, 11), Code = "Other" };
		Assert.Equal((2, "other", false, 8m, 9L, (byte)10), updatedSix.Value);
		Assert.Equal((2, "other", false, 8m, 9L, (byte)10, (short)11), updatedSeven.Value);
		Assert.Same(six.Properties, updatedSix.Properties);
		Assert.Same(seven.Properties, updatedSeven.Properties);
		Assert.Equal(six, six with { Label = "Other" });
		Assert.Equal(seven, seven with { Code = "Other" });
		Assert.Equal(seven.GetHashCode(), (seven with { Code = "Other" }).GetHashCode());
		Assert.NotEqual(six, updatedSix);
		Assert.NotEqual(seven, updatedSeven);
		Roundtrip(updatedSix);
		Roundtrip(updatedSeven);

		var dictionary = new Dictionary<Identifier<int, string, bool, decimal, long, byte, short>, string> { [seven] = "value" };
		var copy = JsonSerializer.Deserialize<Dictionary<Identifier<int, string, bool, decimal, long, byte, short>, string>>(JsonSerializer.Serialize(dictionary));
		Assert.Equal("value", copy[seven]);
		Assert.False(Identifier<int, string, bool, decimal, long, byte>.TryParse(seven.ToString("F"), out _));
		Assert.False(Identifier<int, string, bool, decimal, long, byte, short>.TryParse(six.ToString("F"), out _));
		Assert.Throws<ArgumentNullException>(() => Identifier<int, string, bool, decimal, long, byte>.Parse((string)null));
		Assert.True(Identifier<int, string, bool, decimal, long, byte, short>.Parse(string.Empty).IsEmpty);
		Roundtrip(default(Identifier<int, string, bool, decimal, long, byte>));
		Roundtrip(default(Identifier<int, string, bool, decimal, long, byte, short>));
	}

	[Fact]
	public void TestSixAndSevenGenericFormatting()
	{
		Identifier.Formatting.Register<int, string, bool, decimal, long, byte>("Test.GenericSix", '/');
		Identifier.Formatting.Register<int, string, bool, decimal, long, byte, short>("Test.GenericSeven", '/');
		Identifier.Formatting.Register<int, string, bool, decimal, long, byte>("SixKeyScope", '/');
		Identifier.Formatting.Register<int, string, bool, decimal, long, byte, short>("SevenKeyScope", '/');
		var six = new Identifier<int, string, bool, decimal, long, byte>("Test.GenericSix", 1, "a/b", true, 4m, 5L, 6);
		var seven = new Identifier<int, string, bool, decimal, long, byte, short>("Test.GenericSeven", 1, "a/b", true, 4m, 5L, 6, 7);
		var typedSix = new Identifier<int, string, bool, decimal, long, byte>("SixKeyScope", six.Value);
		var typedSeven = new Identifier<int, string, bool, decimal, long, byte, short>("SevenKeyScope", seven.Value);
		Assert.StartsWith("Test.GenericSix:1/a\\/b/", six.ToString());
		Assert.StartsWith("Test.GenericSeven:1/a\\/b/", seven.ToString());
		Assert.StartsWith("Test.GenericSeven:<int,string,bool,decimal,long,byte,short>", seven.ToString("F"));
		Roundtrip(six);
		Roundtrip(seven);
		Roundtrip(typedSix);
		Roundtrip(typedSeven);
		Roundtrip((Identifier)seven);
		Assert.Equal(seven, Identifier<int, string, bool, decimal, long, byte, short>.Parse(seven.ToString("F")));
		Assert.Equal((Identifier)seven, Identifier.Parse(seven.ToString()));
		Identifier.Formatting.Register<int, string, bool, decimal, long, byte>("Test.GenericSix", '/');
		Assert.Throws<InvalidOperationException>(() => Identifier.Formatting.Register<int, string, bool, decimal, long, byte, short>("Test.GenericSix", '/'));
		Assert.Throws<ArgumentException>(() => Identifier.Formatting.Register("Test.TooMany", '/', Enumerable.Repeat(typeof(int), 8).ToArray()));
	}

	[Fact]
	public void TestValidationPredicates()
	{
		var machine = new Identifier<uint>("Machine", 17);
		Assert.True(machine.Validate("machine", id => id == 17));
		Assert.False(machine.Validate("Machine", id => id == 18));
		Assert.True(new Identifier<uint>("Exception", 17).Validate("Exception", id => id == 17));

		var branch = new Identifier<uint, uint>("Branch", 1, 0);
		Assert.True(branch.Validate("Branch", (tenantId, _) => tenantId != 0));
		Assert.False(branch.Validate("Branch", (tenantId, branchId) => tenantId != 0 && branchId != 0));
		Assert.True(new Identifier<uint, ushort>("IdentifierTest", 1, 2).Validate("IdentifierTest", (tenantId, storeId) => tenantId == 1 && storeId == 2));

		Assert.True(new Identifier<int, string, bool>("Three", 3, "Third", true).Validate("Three", (id, name, enabled) => id == 3 && name == "Third" && enabled));
		Assert.True(new Identifier<int, string, bool>("IdentifierTest", 3, "Third", true).Validate("IdentifierTest", (id, name, enabled) => id == 3 && name == "Third" && enabled));
		Assert.True(new Identifier<int, string, bool, decimal>("Four", 4, "Fourth", false, 12.34m).Validate("Four", (id, name, enabled, amount) => id == 4 && name == "Fourth" && !enabled && amount == 12.34m));
		Assert.True(new Identifier<int, string, bool, decimal>("IdentifierTest", 4, "Fourth", false, 12.34m).Validate("IdentifierTest", (id, name, enabled, amount) => id == 4 && name == "Fourth" && !enabled && amount == 12.34m));
		Assert.True(new Identifier<int, string, bool, decimal, char>("Five", 5, "Fifth", true, 56.78m, 'A').Validate("Five", (id, name, enabled, amount, code) => id == 5 && name == "Fifth" && enabled && amount == 56.78m && code == 'A'));
		Assert.True(new Identifier<int, string, bool, decimal, char>("IdentifierTest", 5, "Fifth", true, 56.78m, 'A').Validate("IdentifierTest", (id, name, enabled, amount, code) => id == 5 && name == "Fifth" && enabled && amount == 56.78m && code == 'A'));
	}

	[Fact]
	public void TestValidationPredicateInvocation()
	{
		var calls = 0;
		Func<uint, uint, bool> predicate = (tenantId, branchId) =>
		{
			calls++;
			return tenantId == 1 && branchId == 1001;
		};
		var branch = new Identifier<uint, uint>("Branch", 1, 1001);
		Assert.False(branch.Validate("Store", predicate));
		Assert.False(branch.Validate("IdentifierTest", predicate));
		Assert.False(default(Identifier<uint, uint>).Validate("Branch", predicate));
		Assert.Equal(0, calls);
		Assert.True(branch.Validate("Branch", predicate));
		Assert.Equal(1, calls);

		var typed = new Identifier<uint, uint>("IdentifierTest", 1, 1001);
		Assert.False(typed.Validate("Exception", predicate));
		Assert.Equal(1, calls);
		Assert.True(typed.Validate("IdentifierTest", predicate));
		Assert.Equal(2, calls);
		Assert.Throws<ArgumentNullException>(() => branch.Validate("Branch", null));
		Assert.Throws<ArgumentNullException>(() => typed.Validate("IdentifierTest", null));
		var exception = new InvalidOperationException();
		Assert.Same(exception, Assert.Throws<InvalidOperationException>(() => branch.Validate("Branch", (_, _) => throw exception)));
	}

	[Fact]
	public void TestDeconstruct()
	{
		(uint tenantId, ushort storeId) = new Identifier<uint, ushort>("Store", 1, 2);
		Assert.Equal((1u, (ushort)2), (tenantId, storeId));

		var (code, label, enabled) = new Identifier<int, string, bool>("Three", 17, "Entry", true);
		Assert.Equal((17, "Entry", true), (code, label, enabled));

		var (id, name, active, amount) = new Identifier<int, string, bool, decimal>("Four", 18, "Item", false, 12.34m);
		Assert.Equal((18, "Item", false, 12.34m), (id, name, active, amount));

		var (year, month, day, hour, minute) = new Identifier<int, byte, byte, byte, byte>("Five", 2026, 10, 30, 12, 34);
		Assert.Equal((2026, (byte)10, (byte)30, (byte)12, (byte)34), (year, month, day, hour, minute));

		var (emptyId, emptyName) = default(Identifier<uint, string>);
		Assert.Equal(0u, emptyId);
		Assert.Null(emptyName);
	}

	[Fact]
	public void TestInvariantScalars()
	{
		var values = new object[] { uint.MaxValue, long.MinValue, ulong.MaxValue, decimal.MaxValue, double.Epsilon, float.MaxValue,
			DateTime.SpecifyKind(new DateTime(2026, 10, 30, 12, 0, 0), DateTimeKind.Utc),
			new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.FromHours(8)),
			new DateOnly(2026, 10, 30), new TimeOnly(12, 34, 56), TimeSpan.FromTicks(-123456789), Guid.NewGuid(), DayOfWeek.Friday, true, ':' };
		foreach(var value in values)
		{
			var identifier = new Identifier("Scalar", value);
			var text = identifier.ToString(null, CultureInfo.GetCultureInfo("fr-FR"));
			Assert.Equal(identifier, Identifier.Parse(text, CultureInfo.GetCultureInfo("de-DE")));
			Assert.Equal(identifier, JsonSerializer.Deserialize<Identifier>(JsonSerializer.Serialize(identifier)));
		}
	}

	[Fact]
	public void TestGenericInterfaceBridge()
	{
		var model = new IdentifiedModel<uint>();
		IIdentifiable contract = model;
		contract.Identifier = new Identifier("Example", "17", (null, "Alias"), "Label", null, new Dictionary<string, string>());
		Assert.Equal(17u, model.Identifier.Value);
		Assert.Equal("Label", contract.Identifier.Label);
		Assert.Equal("Alias", contract.Identifier.Alias);
		Assert.Same(model.Identifier.Properties, contract.Identifier.Properties);
	}

	[Fact]
	public void TestEnumInterfaceBridge()
	{
		var model = new IdentifiedModel<DayOfWeek>();
		IIdentifiable contract = model;
		var identifier = new Identifier("Day", DayOfWeek.Friday, (null, "Fri"), "Friday", null, new Dictionary<string, string>());
		contract.Identifier = identifier;
		Assert.Equal(DayOfWeek.Friday, model.Identifier.Value);
		Assert.Equal(identifier, contract.Identifier);
		Assert.Equal("Friday", model.Identifier.Label);
		Assert.Equal("Fri", model.Identifier.Alias);
		Assert.Same(identifier.Properties, model.Identifier.Properties);
		Roundtrip(model.Identifier);
		contract.Identifier = default;
		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void TestSecurityIdentity()
	{
		Identifier identifier = Security.Privileges.Member.User(17u);
		Assert.Equal(Security.Privileges.Users.Identifiers.User, identifier.Name);
		Assert.Equal(17u, identifier.Value);
		Assert.Equal("Zongsoft.Security.User:<uint>17", identifier.ToString());
		Assert.Equal(identifier, Identifier.Parse(identifier.ToString()));
		Assert.False(identifier.Validate(Security.Privileges.Roles.Identifiers.Role));
		IIdentifiable member = Security.Privileges.Member.User(17u);
		Assert.Throws<NotSupportedException>(() => member.Identifier = default);
	}

	[Fact]
	public void TestComponentAccessAndReplacement()
	{
		var properties = new Dictionary<string, string> { ["region"] = "east" };
		var branch = new Identifier<uint, ushort>("Branch", 17, 23, ("B23", "B", "Branch label"), "Description", properties);
		Assert.True(branch.TryGetValue("branch", 0, out uint tenantId));
		Assert.Equal(17u, tenantId);
		Assert.True(branch.TryGetValue(1, out ushort branchId));
		Assert.Equal((ushort)23, branchId);
		Assert.True(branch.TryGetValue("Branch", out var key));
		Assert.Equal((17u, (ushort)23), key);
		Assert.False(branch.TryGetValue("Store", 0, out tenantId));
		Assert.Equal(0u, tenantId);
		Assert.False(branch.TryGetValue(-1, out tenantId));
		Assert.False(branch.TryGetValue(2, out tenantId));
		Assert.False(default(Identifier<uint, ushort>).TryGetValue(0, out tenantId));

		var changed = branch.With(0, 42).With(1, "7");
		Assert.Equal((42u, (ushort)7), changed.Value);
		Assert.Equal((17u, (ushort)23), branch.Value);
		Assert.Equal(branch.Name, changed.Name);
		Assert.Equal("B23", changed.Code);
		Assert.Equal("Branch label", changed.Label);
		Assert.Equal("B", changed.Alias);
		Assert.Equal("Description", changed.Description);
		Assert.Same(branch.Properties, changed.Properties);
		changed.Properties["region"] = "west";
		Assert.Equal("west", branch.Properties["region"]);
		Assert.Equal("east", properties["region"]);
		Roundtrip(changed);
		Assert.Throws<ArgumentOutOfRangeException>(() => branch.With(-1, 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => branch.With(2, 1));
		Assert.Throws<InvalidCastException>(() => branch.With(1, "invalid"));
		Assert.Throws<InvalidCastException>(() => branch.With(1, 65536));
		Assert.Throws<InvalidCastException>(() => branch.With(1, null));
		Assert.Throws<InvalidOperationException>(() => default(Identifier<uint, uint>).With(0, 1));
	}

	[Fact]
	public void TestNamedCategoryComponentAccess()
	{
		Identifier identifier = new Identifier<uint, ushort>("Example", 17, 23);
		Assert.True(identifier.TryGetValue("Example", 1, out ushort value));
		Assert.Equal((ushort)23, value);
		Assert.False(identifier.TryGetValue("IDisposable", 1, out value));
		Assert.False(identifier.TryGetValue("Exception", 1, out value));
		Assert.Equal(default, value);
		var changed = identifier.With(0, 41);
		Assert.Equal("Example", changed.Name);
		Assert.Equal((41u, (ushort)23), changed.Value);
		Assert.True(changed.TryGetValue<(uint, ushort)>("Example", out var key));
		Assert.Equal((41u, (ushort)23), key);
	}

	[Fact]
	public void TestScalarAndAllGenericComponentCounts()
	{
		var scalar = new Identifier<object>("Scalar", 17u).With(0, 23);
		Assert.IsType<uint>(scalar.Value);
		Assert.True(scalar.TryGetValue(0, out uint value));
		Assert.Equal(23u, value);
		Assert.False(scalar.TryGetValue(1, out value));
		Assert.Throws<ArgumentOutOfRangeException>(() => scalar.With(1, 1));
		Assert.Equal((1u, (ushort)9), new Identifier<uint, ushort>("Two", 1, 2).With(1, 9).Value);
		Assert.Equal((1, "changed", true), new Identifier<int, string, bool>("Three", 1, "two", true).With(1, "changed").Value);
		Assert.Equal((1, "two", false, 4m), new Identifier<int, string, bool, decimal>("Four", 1, "two", true, 4m).With(2, false).Value);
		var five = new Identifier<int, string, bool, decimal, long>("Five", 1, "two", true, 4m, 5L).With(4, 6);
		Assert.Equal((1, "two", true, 4m, 6L), five.Value);
		Assert.True(five.TryGetValue("Five", 4, out long last));
		Assert.Equal(6L, last);
		var tuple = new Identifier<(uint, ushort)>("Tuple", (1u, (ushort)2)).With(0, 9);
		Assert.Equal((9u, (ushort)2), tuple.Value);
		Assert.True(tuple.TryGetValue(1, out ushort second));
		Assert.Equal((ushort)2, second);
	}

	[Fact]
	public void TestConstructorMetadataGroups()
	{
		var properties = new Dictionary<string, string> { ["zone"] = "east" };
		Identifier[] basic =
		[
			new Identifier("Example", 1, "L", "D", properties),
			new Identifier<int>("Example", 1, "L", "D", properties),
			new Identifier<int, string>("Example", 1, "two", "L", "D", properties),
			new Identifier<int, string>("Example", (1, "two"), "L", "D", properties),
			new Identifier<int, string, bool>("Example", 1, "two", true, "L", "D", properties),
			new Identifier<int, string, bool>("Example", (1, "two", true), "L", "D", properties),
			new Identifier<int, string, bool, decimal>("Example", 1, "two", true, 4m, "L", "D", properties),
			new Identifier<int, string, bool, decimal>("Example", (1, "two", true, 4m), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long>("Example", 1, "two", true, 4m, 5L, "L", "D", properties),
			new Identifier<int, string, bool, decimal, long>("Example", (1, "two", true, 4m, 5L), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte>("Example", 1, "two", true, 4m, 5L, (byte)6, "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte>("Example", (1, "two", true, 4m, 5L, (byte)6), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte, short>("Example", 1, "two", true, 4m, 5L, (byte)6, (short)7, "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte, short>("Example", (1, "two", true, 4m, 5L, (byte)6, (short)7), "L", "D", properties),
		];
		Identifier[] pair =
		[
			new Identifier("Example", 1, ("C", "A"), "L", "D", properties),
			new Identifier<int>("Example", 1, ("C", "A"), "L", "D", properties),
			new Identifier<int, string>("Example", 1, "two", ("C", "A"), "L", "D", properties),
			new Identifier<int, string>("Example", (1, "two"), ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool>("Example", 1, "two", true, ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool>("Example", (1, "two", true), ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal>("Example", 1, "two", true, 4m, ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal>("Example", (1, "two", true, 4m), ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long>("Example", 1, "two", true, 4m, 5L, ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long>("Example", (1, "two", true, 4m, 5L), ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte>("Example", 1, "two", true, 4m, 5L, (byte)6, ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte>("Example", (1, "two", true, 4m, 5L, (byte)6), ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte, short>("Example", 1, "two", true, 4m, 5L, (byte)6, (short)7, ("C", "A"), "L", "D", properties),
			new Identifier<int, string, bool, decimal, long, byte, short>("Example", (1, "two", true, 4m, 5L, (byte)6, (short)7), ("C", "A"), "L", "D", properties),
		];
		Identifier[] triple =
		[
			new Identifier("Example", 1, ("C", "A", "L"), "D", properties),
			new Identifier<int>("Example", 1, ("C", "A", "L"), "D", properties),
			new Identifier<int, string>("Example", 1, "two", ("C", "A", "L"), "D", properties),
			new Identifier<int, string>("Example", (1, "two"), ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool>("Example", 1, "two", true, ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool>("Example", (1, "two", true), ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal>("Example", 1, "two", true, 4m, ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal>("Example", (1, "two", true, 4m), ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal, long>("Example", 1, "two", true, 4m, 5L, ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal, long>("Example", (1, "two", true, 4m, 5L), ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal, long, byte>("Example", 1, "two", true, 4m, 5L, (byte)6, ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal, long, byte>("Example", (1, "two", true, 4m, 5L, (byte)6), ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal, long, byte, short>("Example", 1, "two", true, 4m, 5L, (byte)6, (short)7, ("C", "A", "L"), "D", properties),
			new Identifier<int, string, bool, decimal, long, byte, short>("Example", (1, "two", true, 4m, 5L, (byte)6, (short)7), ("C", "A", "L"), "D", properties),
		];

		properties["zone"] = "west";
		Verify(basic, null, null);
		Verify(pair, "C", "A");
		Verify(triple, "C", "A");

		void Verify(Identifier[] identifiers, string code, string alias)
		{
			foreach(var identifier in identifiers)
			{
				Assert.Equal("Example", identifier.Name);

				Assert.Equal(code, identifier.Code);
				Assert.Equal("L", identifier.Label);
				Assert.Equal(alias, identifier.Alias);
				Assert.Equal("D", identifier.Description);
				Assert.NotSame(properties, identifier.Properties);
				Assert.Equal("east", identifier.Properties["ZONE"]);
				var converted = (Identifier<object>)identifier;
				Assert.Equal(code, converted.Code);
				Assert.Equal("L", converted.Label);
				Assert.Equal(alias, converted.Alias);
				Assert.Equal("D", converted.Description);
				Assert.Same(identifier.Properties, converted.Properties);
				Roundtrip(identifier);
			}
		}
	}

	[Fact]
	public void TestTypedInitPreservesIdentityAndMetadata()
	{
		var properties = new Dictionary<string, string> { ["zone"] = "east" };
		var source1 = new Identifier<int>("Example", 1);
		source1 = source1 with
		{
			Properties = properties,
			Code = "C",
			Label = "L",
			Alias = "A",
			Description = "D",
		};
		var updated1 = source1 with { Value = 2, Label = "Updated", Code = null, Alias = null, Description = null };
		Assert.Equal(2, updated1.Value);
		Assert.Equal(source1, source1 with { Label = "Other" });
		Verify(source1, updated1);
		var source2 = new Identifier<int, string>("Example", (1, "two"));
		source2 = source2 with
		{
			Properties = properties,
			Code = "C",
			Label = "L",
			Alias = "A",
			Description = "D",
		};
		var updated2 = source2 with { Value = (2, "other"), Label = "Updated", Code = null, Alias = null, Description = null };
		Assert.Equal((2, "other"), updated2.Value);
		Assert.Equal(source2, source2 with { Label = "Other" });
		Verify(source2, updated2);
		var source3 = new Identifier<int, string, bool>("Example", (1, "two", true));
		source3 = source3 with
		{
			Properties = properties,
			Code = "C",
			Label = "L",
			Alias = "A",
			Description = "D",
		};
		var updated3 = source3 with { Value = (2, "other", false), Label = "Updated", Code = null, Alias = null, Description = null };
		Assert.Equal((2, "other", false), updated3.Value);
		Assert.Equal(source3, source3 with { Label = "Other" });
		Verify(source3, updated3);
		var source4 = new Identifier<int, string, bool, decimal>("Example", (1, "two", true, 4m));
		source4 = source4 with
		{
			Properties = properties,
			Code = "C",
			Label = "L",
			Alias = "A",
			Description = "D",
		};
		var updated4 = source4 with { Value = (2, "other", false, 8m), Label = "Updated", Code = null, Alias = null, Description = null };
		Assert.Equal((2, "other", false, 8m), updated4.Value);
		Assert.Equal(source4, source4 with { Label = "Other" });
		Verify(source4, updated4);
		var source5 = new Identifier<int, string, bool, decimal, long>("Example", (1, "two", true, 4m, 5L));
		source5 = source5 with
		{
			Properties = properties,
			Code = "C",
			Label = "L",
			Alias = "A",
			Description = "D",
		};
		var updated5 = source5 with { Value = (2, "other", false, 8m, 9L), Label = "Updated", Code = null, Alias = null, Description = null };
		Assert.Equal((2, "other", false, 8m, 9L), updated5.Value);
		Assert.Equal(source5, source5 with { Label = "Other" });
		Verify(source5, updated5);

		void Verify(Identifier source, Identifier updated)
		{
			Assert.Equal("Example", updated.Name);

			Assert.Equal("C", source.Code);
			Assert.Equal("L", source.Label);
			Assert.Equal("A", source.Alias);
			Assert.Equal("D", source.Description);
			Assert.Null(updated.Code);
			Assert.Null(updated.Alias);
			Assert.Null(updated.Description);
			Assert.Equal("Updated", updated.Label);
			Assert.NotSame(properties, source.Properties);
			Assert.Same(source.Properties, updated.Properties);
			Assert.NotEqual(source, updated);
			Assert.Equal("east", updated.Properties["ZONE"]);
			Roundtrip(updated);
		}
	}

	[Fact]
	public void TestInitCanFillKnownCategoryWithMissingKey()
	{
		var empty = new Identifier<string>("Example", null) { Label = "Display" };
		Assert.True(empty.IsEmpty);
		var filled = empty with { Value = "key" };
		Assert.Equal("key", filled.Value);
		Assert.Equal("Display", filled.Label);
		Assert.Equal("Example", filled.Name);
		Assert.False(filled.IsEmpty);
		Assert.Null(empty.Value);
	}

	private sealed class IdentifiedModel<T> : IIdentifiable<T>
	{
		public Identifier<T> Identifier { get; set; }
	}

	private static void Roundtrip<T>(T identifier) where T : struct, ISpanParsable<T>
	{
		var text = identifier.ToString();
		AssertResult(T.Parse(text, null));
		AssertResult(T.Parse(text.AsSpan(), null));
		AssertResult(JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(identifier)));
		var converter = TypeDescriptor.GetConverter(typeof(T));
		Assert.Equal(text, converter.ConvertToInvariantString(identifier));
		AssertResult((T)converter.ConvertFromInvariantString(text));

		void AssertResult(T result)
		{
			Assert.Equal(identifier, result);
			Assert.Equal(text, result.ToString());
		}
	}

	private sealed class SixKeyScope { }
	private sealed class SevenKeyScope { }

	private class Person
	{
		public Identifier Id { get; set; }
		public string Name { get; set; }
	}
}
