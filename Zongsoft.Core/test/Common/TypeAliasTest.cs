﻿using System;
using System.Collections.Generic;

using Zongsoft.Tests;

using Xunit;

namespace Zongsoft.Common.Tests;

public class TypeAliasTest
{
	[Fact]
	public void TestParse()
	{
		Assert.Same(typeof(void), TypeAlias.Parse("void"));
		Assert.Same(typeof(object), TypeAlias.Parse(" object"));
		Assert.Same(typeof(object[]), TypeAlias.Parse("object []"));
		Assert.Same(typeof(object), TypeAlias.Parse("System.object"));
		Assert.Same(typeof(object[]), TypeAlias.Parse("System.object []"));

		Assert.Same(typeof(string), TypeAlias.Parse(" string "));
		Assert.Same(typeof(string[]), TypeAlias.Parse("string[ ]"));
		Assert.Same(typeof(string), TypeAlias.Parse("System.string"));
		Assert.Same(typeof(string[]), TypeAlias.Parse("System.string [ ]"));

		Assert.Same(typeof(int), TypeAlias.Parse(" int "));
		Assert.Same(typeof(int), TypeAlias.Parse("int32"));
		Assert.Same(typeof(int), TypeAlias.Parse("System.Int32"));
		Assert.Same(typeof(int?), TypeAlias.Parse("int? "));
		Assert.Same(typeof(int[]), TypeAlias.Parse("int [ ] "));
		Assert.Same(typeof(int?[]), TypeAlias.Parse(" int?[]"));

		Assert.Same(typeof(float), TypeAlias.Parse(" float "));
		Assert.Same(typeof(float), TypeAlias.Parse("Single"));
		Assert.Same(typeof(float), TypeAlias.Parse("System.Single"));
		Assert.Same(typeof(float?), TypeAlias.Parse("float? "));
		Assert.Same(typeof(float[]), TypeAlias.Parse(" float [ ] "));
		Assert.Same(typeof(float?[]), TypeAlias.Parse(" single?[]"));

		Assert.Same(typeof(Guid), TypeAlias.Parse(" GUID"));
		Assert.Same(typeof(Guid), TypeAlias.Parse(" system.guid"));
		Assert.Same(typeof(Guid?), TypeAlias.Parse("guid? "));
		Assert.Same(typeof(Guid[]), TypeAlias.Parse(" guid [] "));
		Assert.Same(typeof(Guid?[]), TypeAlias.Parse("guid?  [ ] "));

		Assert.Same(typeof(DateTime), TypeAlias.Parse("datetime"));
		Assert.Same(typeof(DateTime?), TypeAlias.Parse(" datetime?"));
		Assert.Same(typeof(DateTime[]), TypeAlias.Parse("datetime[]"));
		Assert.Same(typeof(DateTime?[]), TypeAlias.Parse("datetime? [ ]"));

		Assert.Same(typeof(DateOnly), TypeAlias.Parse("date"));
		Assert.Same(typeof(DateOnly), TypeAlias.Parse("dateOnly"));
		Assert.Same(typeof(DateOnly), TypeAlias.Parse("System.DateOnly"));
		Assert.Same(typeof(DateOnly?), TypeAlias.Parse("Date?"));
		Assert.Same(typeof(DateOnly?), TypeAlias.Parse("DateOnly?"));
		Assert.Same(typeof(DateOnly[]), TypeAlias.Parse("Date[]"));
		Assert.Same(typeof(DateOnly[]), TypeAlias.Parse("DateOnly[]"));
		Assert.Same(typeof(DateOnly?[]), TypeAlias.Parse("Date?[]"));
		Assert.Same(typeof(DateOnly?[]), TypeAlias.Parse("DateOnly?[]"));

		Assert.Same(typeof(TimeOnly), TypeAlias.Parse("time"));
		Assert.Same(typeof(TimeOnly), TypeAlias.Parse("timeOnly"));
		Assert.Same(typeof(TimeOnly), TypeAlias.Parse("System.TimeOnly"));
		Assert.Same(typeof(TimeOnly?), TypeAlias.Parse("Time?"));
		Assert.Same(typeof(TimeOnly?), TypeAlias.Parse("TimeOnly?"));
		Assert.Same(typeof(TimeOnly[]), TypeAlias.Parse("Time[]"));
		Assert.Same(typeof(TimeOnly[]), TypeAlias.Parse("TimeOnly[]"));
		Assert.Same(typeof(TimeOnly?[]), TypeAlias.Parse("Time?[]"));
		Assert.Same(typeof(TimeOnly?[]), TypeAlias.Parse("TimeOnly?[]"));

		Assert.Same(typeof(TimeSpan), TypeAlias.Parse("TimeSpan"));
		Assert.Same(typeof(TimeSpan), TypeAlias.Parse("System.TimeSpan"));
		Assert.Same(typeof(TimeSpan?), TypeAlias.Parse("timespan?"));
		Assert.Same(typeof(TimeSpan[]), TypeAlias.Parse("timeSpan[]"));
		Assert.Same(typeof(TimeSpan?[]), TypeAlias.Parse("timeSpan?[]"));

		Assert.Same(typeof(Gender), TypeAlias.Parse("Zongsoft.Tests.Gender,Zongsoft.Core.Tests"));
		Assert.Same(typeof(Gender?), TypeAlias.Parse("Zongsoft.Tests.Gender?, Zongsoft.Core.Tests"));
		Assert.Same(typeof(Gender[]), TypeAlias.Parse("Zongsoft.Tests.Gender[ ],Zongsoft.Core.Tests"));
		Assert.Same(typeof(Gender?[]), TypeAlias.Parse("Zongsoft.Tests.Gender? [ ], Zongsoft.Core.Tests"));

		Assert.Same(typeof(Gender), TypeAlias.Parse("Zongsoft.Tests.Gender@Zongsoft.Core.Tests"));
		Assert.Same(typeof(Gender?), TypeAlias.Parse("Zongsoft.Tests.Gender?@Zongsoft.Core.Tests"));
		Assert.Same(typeof(Gender[]), TypeAlias.Parse("Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests"));
		Assert.Same(typeof(Gender?[]), TypeAlias.Parse("Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests"));

		Assert.Same(typeof(IEnumerable<Gender>), TypeAlias.Parse("IEnumerable<Zongsoft.Tests.Gender@Zongsoft.Core.Tests>"));
		Assert.Same(typeof(IEnumerable<Gender?>), TypeAlias.Parse("IEnumerable<Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>"));
		Assert.Same(typeof(IEnumerable<Gender[]>), TypeAlias.Parse("System.Collections.Generic.IEnumerable<Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>"));
		Assert.Same(typeof(IEnumerable<Gender?[]>), TypeAlias.Parse("System.Collections.Generic.IEnumerable<Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>"));

		Assert.Same(typeof(IDictionary<string, Gender>), TypeAlias.Parse("IDictionary<string, Zongsoft.Tests.Gender@Zongsoft.Core.Tests>"));
		Assert.Same(typeof(IDictionary<string, Gender?>), TypeAlias.Parse("IDictionary<string, Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>"));
		Assert.Same(typeof(IDictionary<string, Gender[]>), TypeAlias.Parse("System.Collections.Generic.IDictionary<string, Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>"));
		Assert.Same(typeof(IDictionary<string, Gender?[]>), TypeAlias.Parse("System.Collections.Generic.IDictionary<string, Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>"));

		var tupleType = typeof(Tuple<ValueTuple<Zongsoft.Data.Range<int>>, ValueTuple<Zongsoft.Data.Range<DateTime>?>, ValueTuple<Zongsoft.Data.Range<DateTime>[]>, ValueTuple<Zongsoft.Data.Range<DateTime>?[]>>);
		Assert.Same(tupleType, TypeAlias.Parse(tupleType.FullName));
		Assert.Same(tupleType, TypeAlias.Parse("Tuple<ValueTuple<Range<int>>, ValueTuple<Zongsoft.Data.Range<DateTime>?>, ValueTuple<Zongsoft.Data.Range<DateTime>[]>, ValueTuple<Range<DateTime>?[]>>"));

		tupleType = typeof(ValueTuple<string, DateOnly?, byte[], Guid?[], Zongsoft.Data.Range<DateTime>?[], Zongsoft.Data.ConditionOperator?[]>);
		Assert.Same(tupleType, TypeAlias.Parse(tupleType.FullName));
		Assert.Same(tupleType, TypeAlias.Parse("ValueTuple<string, date? ,binary , guid?[], RANGE<datetime>?[], Zongsoft.Data.ConditionOperator? [ ]@Zongsoft.Core >"));
	}

	[Fact]
	public void TestGetAlias()
	{
		Assert.Equal("object", TypeAlias.GetAlias(typeof(object)), true);
		Assert.Equal("object[]", TypeAlias.GetAlias(typeof(object[])), true);
		Assert.Equal("DBNull", TypeAlias.GetAlias(typeof(DBNull)), true);
		Assert.Equal("DBNull[]", TypeAlias.GetAlias(typeof(DBNull[])), true);

		Assert.Equal("void", TypeAlias.GetAlias(typeof(void)), true);
		Assert.Equal("string", TypeAlias.GetAlias(typeof(string)), true);
		Assert.Equal("string[]", TypeAlias.GetAlias(typeof(string[])), true);

		Assert.Equal("int32", TypeAlias.GetAlias(typeof(int)), true);
		Assert.Equal("int32?", TypeAlias.GetAlias(typeof(int?)), true);
		Assert.Equal("int32[]", TypeAlias.GetAlias(typeof(int[])), true);
		Assert.Equal("int32?[]", TypeAlias.GetAlias(typeof(int?[])), true);

		Assert.Equal("Single", TypeAlias.GetAlias(typeof(float)), true);
		Assert.Equal("Single?", TypeAlias.GetAlias(typeof(float?)), true);
		Assert.Equal("Single[]", TypeAlias.GetAlias(typeof(float[])), true);
		Assert.Equal("Single?[]", TypeAlias.GetAlias(typeof(float?[])), true);

		Assert.Equal("Date", TypeAlias.GetAlias(typeof(DateOnly)), true);
		Assert.Equal("Date?", TypeAlias.GetAlias(typeof(DateOnly?)), true);
		Assert.Equal("date[]", TypeAlias.GetAlias(typeof(DateOnly[])), true);
		Assert.Equal("date?[]", TypeAlias.GetAlias(typeof(DateOnly?[])), true);

		Assert.Equal("Range<Timestamp>", TypeAlias.GetAlias(typeof(Zongsoft.Data.Range<DateTimeOffset>)), true);
		Assert.Equal("Range<Timestamp>?", TypeAlias.GetAlias(typeof(Zongsoft.Data.Range<DateTimeOffset>?)), true);
		Assert.Equal("Range<Timestamp>[]", TypeAlias.GetAlias(typeof(Zongsoft.Data.Range<DateTimeOffset>[])), true);
		Assert.Equal("Range<Timestamp>?[]", TypeAlias.GetAlias(typeof(Zongsoft.Data.Range<DateTimeOffset>?[])), true);

		Assert.Equal("Zongsoft.Tests.Gender@Zongsoft.Core.Tests", TypeAlias.GetAlias(typeof(Gender)), true);
		Assert.Equal("Zongsoft.Tests.Gender?@Zongsoft.Core.Tests", TypeAlias.GetAlias(typeof(Gender?)), true);
		Assert.Equal("Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests", TypeAlias.GetAlias(typeof(Gender[])), true);
		Assert.Equal("Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests", TypeAlias.GetAlias(typeof(Gender?[])), true);

		Assert.Equal("IEnumerable<Zongsoft.Tests.Gender@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IEnumerable<Gender>)), true);
		Assert.Equal("IEnumerable<Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IEnumerable<Gender?>)), true);
		Assert.Equal("IEnumerable<Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IEnumerable<Gender[]>)), true);
		Assert.Equal("IEnumerable<Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IEnumerable<Gender?[]>)), true);

		Assert.Equal("List<Zongsoft.Tests.Gender@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(List<Gender>)), true);
		Assert.Equal("List<Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(List<Gender?>)), true);
		Assert.Equal("List<Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(List<Gender[]>)), true);
		Assert.Equal("List<Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(List<Gender?[]>)), true);

		Assert.Equal("IDictionary<String, Zongsoft.Tests.Gender@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IDictionary<string, Gender>)), true);
		Assert.Equal("IDictionary<String, Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IDictionary<string, Gender?>)), true);
		Assert.Equal("IDictionary<String, Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IDictionary<string, Gender[]>)), true);
		Assert.Equal("IDictionary<String, Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(IDictionary<string, Gender?[]>)), true);

		Assert.Equal("Dictionary<Range<DateTime>, Zongsoft.Tests.Gender@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(Dictionary<Zongsoft.Data.Range<DateTime>, Gender>)), true);
		Assert.Equal("Dictionary<Range<DateTime>?, Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(Dictionary<Zongsoft.Data.Range<DateTime>?, Gender?>)), true);
		Assert.Equal("Dictionary<Range<DateTime>[], Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(Dictionary<Zongsoft.Data.Range<DateTime>[], Gender[]>)), true);
		Assert.Equal("Dictionary<Range<DateTime>?[], Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>", TypeAlias.GetAlias(typeof(Dictionary<Zongsoft.Data.Range<DateTime>?[], Gender?[]>)), true);

		var tupleType = typeof(Tuple<ValueTuple<Zongsoft.Data.Range<int>>, ValueTuple<Zongsoft.Data.Range<DateTime>?>, ValueTuple<Zongsoft.Data.Range<DateTime>[]>, ValueTuple<Zongsoft.Data.Range<DateTime>?[]>>);
		var tupleAlias = "Tuple<ValueTuple<Range<Int32>>, ValueTuple<Range<DateTime>?>, ValueTuple<Range<DateTime>[]>, ValueTuple<Range<DateTime>?[]>>";
		Assert.Equal(tupleAlias, tupleType.GetAlias());

		tupleType = typeof(ValueTuple<string, DateOnly?, byte[], Guid?[], Zongsoft.Data.Range<DateTime>?[], Zongsoft.Data.ConditionOperator?[]>);
		tupleAlias = "ValueTuple<String, Date?, Byte[], Guid?[], Range<DateTime>?[], Zongsoft.Data.ConditionOperator?[]@Zongsoft.Core>";
		Assert.Equal(tupleAlias, tupleType.GetAlias());

		tupleType = typeof(Nullable<>).MakeGenericType(tupleType);
		tupleAlias += '?';
		Assert.Equal(tupleAlias, tupleType.GetAlias());

		tupleType = tupleType.MakeArrayType();
		tupleAlias += "[]";
		Assert.Equal(tupleAlias, tupleType.GetAlias());
	}
}
