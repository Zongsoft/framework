using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class IdentifiableTest
{
	#region 测试方法
	[Fact]
	public void Test2Components()
	{
		var model = new Model2<uint, string>();
		Verify<(uint, string)>(model, (17u, "two"), ("17", "two"));

		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void Test3Components()
	{
		var model = new Model3<uint, string, DayOfWeek>();
		Verify<(uint, string, DayOfWeek)>(model, (17u, "two", DayOfWeek.Friday), ("17", "two", "Friday"));

		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void Test4Components()
	{
		var model = new Model4<uint, string, DayOfWeek, decimal>();
		Verify<(uint, string, DayOfWeek, decimal)>(model, (17u, "two", DayOfWeek.Friday, 4m), ("17", "two", "Friday", "4"));

		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void Test5Components()
	{
		var model = new Model5<uint, string, DayOfWeek, decimal, long>();
		Verify<(uint, string, DayOfWeek, decimal, long)>(model, (17u, "two", DayOfWeek.Friday, 4m, 5L), ("17", "two", "Friday", "4", "5"));

		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void Test6Components()
	{
		var model = new Model6<uint, string, DayOfWeek, decimal, long, byte>();
		Verify<(uint, string, DayOfWeek, decimal, long, byte)>(model, (17u, "two", DayOfWeek.Friday, 4m, 5L, (byte)6), ("17", "two", "Friday", "4", "5", "6"));

		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void Test7Components()
	{
		var model = new Model7<uint, string, DayOfWeek, decimal, long, byte, short>();
		Verify<(uint, string, DayOfWeek, decimal, long, byte, short)>(model, (17u, "two", DayOfWeek.Friday, 4m, 5L, (byte)6, (short)7), ("17", "two", "Friday", "4", "5", "6", "7"));

		Assert.True(model.Identifier.IsEmpty);
	}

	[Fact]
	public void TestReadOnlyBridge()
	{
		var model = new ReadOnlyModel();
		IIdentifiable<(uint, string)> tuple = model;
		IIdentifiable general = model;

		Assert.Equal((17u, "two"), tuple.Identifier.Value);
		Assert.Equal((17u, "two"), general.Identifier.Value);

		Assert.Throws<NotSupportedException>(() => tuple.Identifier = new("Pair", (23u, "other")));
		Assert.Throws<NotSupportedException>(() => general.Identifier = new("Pair", (23u, "other")));

		Assert.Equal((17u, "two"), model.Identifier.Value);
	}
	#endregion

	#region 验证方法
	private static void Verify<T>(IIdentifiable<T> model, T expected, object convertible)
	{
		IIdentifiable general = model;
		var source = new Identifier("Composite", convertible, ("C", "A"), "Label", "Description", new Dictionary<string, string> { ["zone"] = "east" });

		general.Identifier = source;

		Assert.Equal(expected, model.Identifier.Value);
		Assert.Equal("Composite", general.Identifier.Name);
		Assert.Equal("C", model.Identifier.Code);
		Assert.Equal("A", model.Identifier.Alias);
		Assert.Equal("Label", model.Identifier.Label);
		Assert.Equal("Description", model.Identifier.Description);
		Assert.Same(source.Properties, model.Identifier.Properties);
		Assert.Same(source.Properties, general.Identifier.Properties);

		var identity = general.Identifier;
		Assert.Equal(identity, Identifier.Parse(identity.ToString("F")));

		Assert.Throws<InvalidCastException>(() => general.Identifier = new("Composite", "invalid"));
		Assert.Equal(identity, general.Identifier);

		model.Identifier = model.Identifier with { Label = "Updated" };

		Assert.Equal(expected, general.Identifier.Value);
		Assert.Equal("Updated", general.Identifier.Label);
		Assert.Equal(identity, general.Identifier);
		Assert.Same(identity.Properties, general.Identifier.Properties);

		general.Identifier = default;

		Assert.True(model.Identifier.IsEmpty);
		Assert.True(general.Identifier.IsEmpty);
	}
	#endregion

	#region 测试模型
	private sealed class Model2<T1, T2> : IIdentifiable<T1, T2>
	{
		public Identifier<T1, T2> Identifier { get; set; }
	}

	private sealed class Model3<T1, T2, T3> : IIdentifiable<T1, T2, T3>
	{
		public Identifier<T1, T2, T3> Identifier { get; set; }
	}

	private sealed class Model4<T1, T2, T3, T4> : IIdentifiable<T1, T2, T3, T4>
	{
		public Identifier<T1, T2, T3, T4> Identifier { get; set; }
	}

	private sealed class Model5<T1, T2, T3, T4, T5> : IIdentifiable<T1, T2, T3, T4, T5>
	{
		public Identifier<T1, T2, T3, T4, T5> Identifier { get; set; }
	}

	private sealed class Model6<T1, T2, T3, T4, T5, T6> : IIdentifiable<T1, T2, T3, T4, T5, T6>
	{
		public Identifier<T1, T2, T3, T4, T5, T6> Identifier { get; set; }
	}

	private sealed class Model7<T1, T2, T3, T4, T5, T6, T7> : IIdentifiable<T1, T2, T3, T4, T5, T6, T7>
	{
		public Identifier<T1, T2, T3, T4, T5, T6, T7> Identifier { get; set; }
	}

	private sealed class ReadOnlyModel : IIdentifiable<uint, string>
	{
		public Identifier<uint, string> Identifier
		{
			get => new("Pair", 17u, "two");
			set => throw new NotSupportedException();
		}
	}
	#endregion
}
