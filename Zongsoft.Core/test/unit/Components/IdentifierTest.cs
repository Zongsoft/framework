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
		var identifier = new Identifier<uint>(typeof(IdentifierTest), 100u);
		Assert.True(identifier.HasValue);
		Assert.False(identifier.IsEmpty);
		Assert.Equal(typeof(IdentifierTest), identifier.Type);
		Assert.Null(identifier.Label);
		Assert.Null(identifier.Description);
		Assert.Equal(100u, identifier.Value);
		Assert.Equal(100u, (uint)identifier);

		Assert.True(identifier.Validate(out var value));
		Assert.Equal(100U, value);
		Assert.True(identifier.Validate<IdentifierTest>(out value));
		Assert.Equal(100U, value);
		Assert.False(identifier.Validate<Exception>(out value));
	}

	[Fact]
	public void TestGeneral()
	{
		var identifier = new Identifier(typeof(IdentifierTest), 100U);
		Assert.True(identifier.HasValue);
		Assert.False(identifier.IsEmpty);
		Assert.Equal(typeof(IdentifierTest), identifier.Type);
		Assert.Null(identifier.Label);
		Assert.Null(identifier.Description);
		Assert.Equal(100u, identifier.Value);
		Assert.Equal(100u, (uint)identifier);

		Assert.True(identifier.Validate(out uint value));
		Assert.Equal(100U, value);
		Assert.True(identifier.Validate<IdentifierTest, uint>(out value));
		Assert.Equal(100U, value);
		Assert.False(identifier.Validate<Exception, uint>(out value));
	}

	[Fact]
	public void TestConvertible()
	{
		var identifier = new Identifier(typeof(IdentifierTest), 100);
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
		Assert.NotEqual(new Identifier("Machine", 17u), new Identifier(typeof(Person), 17u));
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
		var typed = new Identifier(typeof(IDisposable), null);
		Assert.True(named.IsEmpty);
		Assert.True(typed.IsEmpty);
		Assert.False(named.Validate("Example"));
		Assert.False(typed.Validate<IDisposable>());
		Assert.False(typed.Validate<IDisposable, uint>(out _));
		Assert.False(typed.Validate(typeof(IDisposable), out uint _));
		Assert.False(default(Identifier).Validate<IDisposable>());
		Assert.True(new Identifier(typeof(InvalidOperationException), 0).Validate<Exception>());

		var calls = 0;
		var generic = new Identifier<string>(typeof(IDisposable), null);
		Assert.False(generic.Validate(typeof(IDisposable), _ => { calls++; return true; }));
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
		Identifier.Formatting.Register<string, int>(typeof(IdentifierTest));
		Identifier.Formatting.Register<string, int>(typeof(IdentifierTest), '-');
		var identifier = new Identifier<string, int>(typeof(IdentifierTest), "A-B", -2);
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
		var identifier = new Identifier("Slices", text, text, text, text, properties, text);
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
		var identifier = new Identifier("Test", "a:b,?()|#{}\\\r\n\t\u0001", "杭州|中心", "Description#{}", "杭运", properties);
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
		Assert.Equal("Alias:1?|A", new Identifier("Alias", "1", alias: "A").ToString());
		Assert.Equal("Description:1(D)", new Identifier("Description", "1", description: "D").ToString());
	}

	[Fact]
	public void TestCodeMetadata()
	{
		var properties = new Dictionary<string, string> { ["Code"] = "Independent property" };
		var identifier = new Identifier("Order", 1001UL, "Order", "Internal (purchase)", "PO", properties, code: " SO#001?()<>|{}\\\r\n ");
		var parsed = Identifier.Parse(identifier.ToString());
		Assert.Equal(identifier.Code, parsed.Code);
		Assert.Equal(identifier.Label, parsed.Label);
		Assert.Equal(identifier.Alias, parsed.Alias);
		Assert.Equal(identifier.Description, parsed.Description);
		Assert.Equal("Independent property", parsed.Properties["Code"]);
		var other = new Identifier("Order", 1001UL, code: "SO002");
		Assert.Equal(identifier, other);
		Assert.Equal(identifier.GetHashCode(), other.GetHashCode());
		Assert.True(identifier.Validate("Order"));
		Assert.Equal(identifier.Code, identifier.Cast<Person>().Code);
		Roundtrip(identifier);

		Assert.Equal("Order:<ulong>1001#SO001", new Identifier("Order", 1001UL, code: "SO001").ToString());
		Assert.Equal("Order:<ulong>1001", new Identifier("Order", 1001UL, code: "").ToString());
		var empty = new Identifier("Order", null, code: "SO001");
		Assert.True(empty.IsEmpty);
		Assert.Equal("null", JsonSerializer.Serialize(empty));
	}

	[Fact]
	public void TestCodeConversions()
	{
		var source = new Identifier<uint, ushort>("Store", 1, 2, "Store", code: "S002", properties: new Dictionary<string, string>());
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
		Roundtrip(new Identifier<uint>(typeof(Person), 1, code: "P001"));
		Roundtrip(new Identifier<int, string, Guid>("Three", 1, "two", Guid.Empty, code: "003"));
		Roundtrip(new Identifier<int, string, bool, decimal>("Four", 1, "two", true, 4m, code: "004"));
		Roundtrip(new Identifier<int, string, bool, decimal, DateOnly>("Five", 1, "two", true, 4m, new(2026, 10, 31), code: "005"));

		var model = new IdentifiedModel<uint>();
		IIdentifiable identifiable = model;
		identifiable.Identifier = new Identifier("Person", 1u, code: "P001");
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
		Roundtrip(new Identifier(typeof(Dictionary<string, List<int>>), 17u, code: "D017", description: "(nested)"));
		Roundtrip(new Identifier("Text", "<uint>17#Code?(description){x=y}|alias", code: "#?()<>"));
		Assert.Equal(17u, Identifier.Parse("$List<int>:<uint>17").Value);
		Assert.Equal(typeof(List<int>), Identifier.Parse("$List<int>:17").Type);
		Assert.IsType<string>(Identifier.Parse("$List<int>:17").Value);
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
		var identifier = new Identifier("Test", 17u, "Label", properties: properties);
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
	[InlineData("A:<int,int,int,int,int,int>1,2,3,4,5,6")]
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
		var source = new Identifier<uint, ushort>("Store", 1, 2, "Name", alias: "Alias", properties: new Dictionary<string, string>());
		Identifier<(uint, ushort)> tuple = source;
		Identifier<uint, ushort> restored = tuple;
		Assert.Equal(source, restored);
		Assert.Equal(tuple, Identifier<(uint, ushort)>.Parse(source.ToString("F")));
		Assert.True(new Identifier("Store", (1, 2)).Validate<(uint, ushort)>(out var converted));
		Assert.Equal((1u, (ushort)2), converted);
		Assert.Same(source.Properties, restored.Properties);
		Assert.Equal((Identifier)source, (Identifier)tuple);
		Assert.False(Identifier<int>.TryParse("One:<uint>1", out _));
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Identifier>("17"));
		Assert.Equal("null", JsonSerializer.Serialize(default(Identifier)));
		Assert.Equal(default, JsonSerializer.Deserialize<Identifier>("null"));
		var typed = new Identifier(typeof(Dictionary<string, Person>), 17u);
		Assert.Equal(typed, Identifier.Parse(typed.ToString()));
	}

	[Fact]
	public void TestValidationPredicates()
	{
		var machine = new Identifier<uint>("Machine", 17);
		Assert.True(machine.Validate("machine", id => id == 17));
		Assert.False(machine.Validate("Machine", id => id == 18));
		Assert.True(new Identifier<uint>(typeof(InvalidOperationException), 17).Validate(typeof(Exception), id => id == 17));

		var branch = new Identifier<uint, uint>("Branch", 1, 0);
		Assert.True(branch.Validate("Branch", (tenantId, _) => tenantId != 0));
		Assert.False(branch.Validate("Branch", (tenantId, branchId) => tenantId != 0 && branchId != 0));
		Assert.True(new Identifier<uint, ushort>(typeof(IdentifierTest), 1, 2).Validate(typeof(IdentifierTest), (tenantId, storeId) => tenantId == 1 && storeId == 2));

		Assert.True(new Identifier<int, string, bool>("Three", 3, "Third", true).Validate("Three", (id, name, enabled) => id == 3 && name == "Third" && enabled));
		Assert.True(new Identifier<int, string, bool>(typeof(IdentifierTest), 3, "Third", true).Validate(typeof(IdentifierTest), (id, name, enabled) => id == 3 && name == "Third" && enabled));
		Assert.True(new Identifier<int, string, bool, decimal>("Four", 4, "Fourth", false, 12.34m).Validate("Four", (id, name, enabled, amount) => id == 4 && name == "Fourth" && !enabled && amount == 12.34m));
		Assert.True(new Identifier<int, string, bool, decimal>(typeof(IdentifierTest), 4, "Fourth", false, 12.34m).Validate(typeof(IdentifierTest), (id, name, enabled, amount) => id == 4 && name == "Fourth" && !enabled && amount == 12.34m));
		Assert.True(new Identifier<int, string, bool, decimal, char>("Five", 5, "Fifth", true, 56.78m, 'A').Validate("Five", (id, name, enabled, amount, code) => id == 5 && name == "Fifth" && enabled && amount == 56.78m && code == 'A'));
		Assert.True(new Identifier<int, string, bool, decimal, char>(typeof(IdentifierTest), 5, "Fifth", true, 56.78m, 'A').Validate(typeof(IdentifierTest), (id, name, enabled, amount, code) => id == 5 && name == "Fifth" && enabled && amount == 56.78m && code == 'A'));
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
		Assert.False(branch.Validate(typeof(IdentifierTest), predicate));
		Assert.False(default(Identifier<uint, uint>).Validate("Branch", predicate));
		Assert.Equal(0, calls);
		Assert.True(branch.Validate("Branch", predicate));
		Assert.Equal(1, calls);

		var typed = new Identifier<uint, uint>(typeof(IdentifierTest), 1, 1001);
		Assert.False(typed.Validate(typeof(Exception), predicate));
		Assert.Equal(1, calls);
		Assert.True(typed.Validate(typeof(IdentifierTest), predicate));
		Assert.Equal(2, calls);
		Assert.Throws<ArgumentNullException>(() => branch.Validate("Branch", null));
		Assert.Throws<ArgumentNullException>(() => typed.Validate(typeof(IdentifierTest), null));
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
		contract.Identifier = new Identifier("Example", "17", "Label", alias: "Alias", properties: new Dictionary<string, string>());
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
		var identifier = new Identifier("Day", DayOfWeek.Friday, "Friday", alias: "Fri", properties: new Dictionary<string, string>());
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
		Assert.Equal(typeof(Security.Privileges.IUser), identifier.Type);
		Assert.Equal(17u, identifier.Value);
		Assert.Equal("$Zongsoft.Security.Privileges.IUser@Zongsoft.Core:<uint>17", identifier.ToString());
		Assert.Equal(identifier, Identifier.Parse(identifier.ToString()));
		Assert.False(identifier.Validate<Security.Privileges.IRole>());
		IIdentifiable member = Security.Privileges.Member.User(17u);
		Assert.Throws<NotSupportedException>(() => member.Identifier = default);
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

	private class Person
	{
		public Identifier Id { get; set; }
		public string Name { get; set; }
	}
}
