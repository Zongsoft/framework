using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Zongsoft.Tests;

using Xunit;

namespace Zongsoft.Common.Tests
{
	public class TypeExtensionTest
	{
		[Fact]
		public void TestIsAssignableFrom()
		{
			var baseType = typeof(ICollection<Person>);
			var instanceType = typeof(Collection<Person>);

			Assert.True(TypeExtension.IsAssignableFrom(baseType, instanceType));
			Assert.True(baseType.IsAssignableFrom(instanceType));

			baseType = typeof(ICollection<>);

			Assert.True(TypeExtension.IsAssignableFrom(baseType, instanceType));
			Assert.False(baseType.IsAssignableFrom(instanceType));

			Assert.True(TypeExtension.IsAssignableFrom(typeof(IService<>), typeof(EmployeeService)));
			Assert.True(TypeExtension.IsAssignableFrom(typeof(PersonServiceBase<>), typeof(EmployeeService)));

			Assert.True(TypeExtension.IsAssignableFrom(typeof(IService<>), typeof(EmployeeService), out var genericTypes));
			Assert.NotEmpty(genericTypes);
			Assert.Equal(1, genericTypes.Count);
			Assert.Same(genericTypes[0], typeof(IService<Employee>));

			Assert.True(TypeExtension.IsAssignableFrom(typeof(PersonServiceBase<>), typeof(EmployeeService), out genericTypes));
			Assert.NotEmpty(genericTypes);
			Assert.Equal(1, genericTypes.Count);
			Assert.Same(genericTypes[0], typeof(PersonServiceBase<Employee>));
		}

		[Fact]
		public void TestTypeAliasParse()
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

			var tupleType = typeof(ValueTuple<string, DateOnly?, byte[], Guid?[], Zongsoft.Data.Range<DateTime>?, Zongsoft.Data.ConditionOperator?[]>);
			Assert.Same(tupleType, TypeAlias.Parse(tupleType.FullName));
			Assert.Same(tupleType, TypeAlias.Parse("ValueTuple<string, date? ,binary , guid?[], RANGE<datetime>?, Zongsoft.Data.ConditionOperator? [ ]@Zongsoft.Core >"));
		}

		[Fact]
		public void TestGetTypeAlias()
		{
			Assert.Equal("int32", TypeAlias.GetAlias(typeof(int)), true);
		}

		[Fact]
		public void TestGetDefaultValue()
		{
			Assert.Equal(0, TypeExtension.GetDefaultValue(typeof(int)));
			Assert.Equal(0d, TypeExtension.GetDefaultValue(typeof(double)));
			Assert.Equal(DBNull.Value, TypeExtension.GetDefaultValue(typeof(DBNull)));
			Assert.Equal(Gender.Female, TypeExtension.GetDefaultValue(typeof(Gender)));
			Assert.Null(TypeExtension.GetDefaultValue(typeof(int?)));
			Assert.Null(TypeExtension.GetDefaultValue(typeof(string)));
		}

		#region 嵌套子类
		internal interface IService<out T>
		{
			T Get(int id);
		}

		internal class PersonServiceBase<T> : IService<T> where T : Person
		{
			public T Get(int id) => this.GetPerson(id);
			public virtual T GetPerson(int id) => throw new NotImplementedException();
		}

		internal class EmployeeService : PersonServiceBase<Employee>
		{
			public override Employee GetPerson(int id) => new Employee(id, "Unnamed");
		}
		#endregion
	}
}
