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
		public void TestGetType()
		{
			Assert.Same(typeof(void), TypeExtension.GetType("void"));
			Assert.Same(typeof(object), TypeExtension.GetType("object"));
			Assert.Same(typeof(object), TypeExtension.GetType("System.object"));

			Assert.Same(typeof(string), TypeExtension.GetType("string"));
			Assert.Same(typeof(string), TypeExtension.GetType("System.string"));

			Assert.Same(typeof(int), TypeExtension.GetType("int"));
			Assert.Same(typeof(int), TypeExtension.GetType("int32"));
			Assert.Same(typeof(int), TypeExtension.GetType("System.Int32"));
			Assert.Same(typeof(int?), TypeExtension.GetType("int?"));
			Assert.Same(typeof(int[]), TypeExtension.GetType("int[]"));
			Assert.Same(typeof(int?[]), TypeExtension.GetType("int?[]"));

			Assert.Same(typeof(Guid), TypeExtension.GetType("GUID"));
			Assert.Same(typeof(Guid), TypeExtension.GetType("system.guid"));
			Assert.Same(typeof(Guid?), TypeExtension.GetType("guid?"));
			Assert.Same(typeof(Guid[]), TypeExtension.GetType("guid[]"));
			Assert.Same(typeof(Guid?[]), TypeExtension.GetType("guid?[]"));

			Assert.Same(typeof(DateTime), TypeExtension.GetType("datetime"));
			Assert.Same(typeof(DateTime?), TypeExtension.GetType("datetime?"));
			Assert.Same(typeof(DateTime[]), TypeExtension.GetType("datetime[]"));
			Assert.Same(typeof(DateTime?[]), TypeExtension.GetType("datetime?[]"));

			Assert.Same(typeof(DateOnly), TypeExtension.GetType("date"));
			Assert.Same(typeof(DateOnly), TypeExtension.GetType("dateOnly"));
			Assert.Same(typeof(DateOnly), TypeExtension.GetType("System.DateOnly"));
			Assert.Same(typeof(DateOnly?), TypeExtension.GetType("Date?"));
			Assert.Same(typeof(DateOnly?), TypeExtension.GetType("DateOnly?"));
			Assert.Same(typeof(DateOnly[]), TypeExtension.GetType("Date[]"));
			Assert.Same(typeof(DateOnly[]), TypeExtension.GetType("DateOnly[]"));
			Assert.Same(typeof(DateOnly?[]), TypeExtension.GetType("Date?[]"));
			Assert.Same(typeof(DateOnly?[]), TypeExtension.GetType("DateOnly?[]"));

			Assert.Same(typeof(TimeOnly), TypeExtension.GetType("time"));
			Assert.Same(typeof(TimeOnly), TypeExtension.GetType("timeOnly"));
			Assert.Same(typeof(TimeOnly), TypeExtension.GetType("System.TimeOnly"));
			Assert.Same(typeof(TimeOnly?), TypeExtension.GetType("Time?"));
			Assert.Same(typeof(TimeOnly?), TypeExtension.GetType("TimeOnly?"));
			Assert.Same(typeof(TimeOnly[]), TypeExtension.GetType("Time[]"));
			Assert.Same(typeof(TimeOnly[]), TypeExtension.GetType("TimeOnly[]"));
			Assert.Same(typeof(TimeOnly?[]), TypeExtension.GetType("Time?[]"));
			Assert.Same(typeof(TimeOnly?[]), TypeExtension.GetType("TimeOnly?[]"));

			Assert.Same(typeof(TimeSpan), TypeExtension.GetType("TimeSpan"));
			Assert.Same(typeof(TimeSpan), TypeExtension.GetType("System.TimeSpan"));
			Assert.Same(typeof(TimeSpan?), TypeExtension.GetType("timespan?"));
			Assert.Same(typeof(TimeSpan[]), TypeExtension.GetType("timeSpan[]"));
			Assert.Same(typeof(TimeSpan?[]), TypeExtension.GetType("timeSpan?[]"));

			Assert.Same(typeof(Gender), TypeExtension.GetType("Zongsoft.Tests.Gender, Zongsoft.Core.Tests"));
			Assert.Same(typeof(Gender?), TypeExtension.GetType("Zongsoft.Tests.Gender?, Zongsoft.Core.Tests"));
			Assert.Same(typeof(Gender[]), TypeExtension.GetType("Zongsoft.Tests.Gender[], Zongsoft.Core.Tests"));
			Assert.Same(typeof(Gender?[]), TypeExtension.GetType("Zongsoft.Tests.Gender?[], Zongsoft.Core.Tests"));

			Assert.Same(typeof(Gender), TypeExtension.GetType("Zongsoft.Tests.Gender@Zongsoft.Core.Tests"));
			Assert.Same(typeof(Gender?), TypeExtension.GetType("Zongsoft.Tests.Gender?@Zongsoft.Core.Tests"));
			Assert.Same(typeof(Gender[]), TypeExtension.GetType("Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests"));
			Assert.Same(typeof(Gender?[]), TypeExtension.GetType("Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests"));

			Assert.Same(typeof(IEnumerable<Gender>), TypeExtension.GetType("IEnumerable<Zongsoft.Tests.Gender@Zongsoft.Core.Tests>"));
			Assert.Same(typeof(IEnumerable<Gender?>), TypeExtension.GetType("IEnumerable<Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>"));
			Assert.Same(typeof(IEnumerable<Gender[]>), TypeExtension.GetType("System.Collections.Generic.IEnumerable<Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>"));
			Assert.Same(typeof(IEnumerable<Gender?[]>), TypeExtension.GetType("System.Collections.Generic.IEnumerable<Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>"));

			Assert.Same(typeof(IDictionary<string, Gender>), TypeExtension.GetType("IDictionary<string, Zongsoft.Tests.Gender@Zongsoft.Core.Tests>"));
			Assert.Same(typeof(IDictionary<string, Gender?>), TypeExtension.GetType("IDictionary<string, Zongsoft.Tests.Gender?@Zongsoft.Core.Tests>"));
			Assert.Same(typeof(IDictionary<string, Gender[]>), TypeExtension.GetType("System.Collections.Generic.IDictionary<string, Zongsoft.Tests.Gender[]@Zongsoft.Core.Tests>"));
			Assert.Same(typeof(IDictionary<string, Gender?[]>), TypeExtension.GetType("System.Collections.Generic.IDictionary<string, Zongsoft.Tests.Gender?[]@Zongsoft.Core.Tests>"));

			var tupleType = typeof(ValueTuple<string, DateOnly?, byte[], Guid?[], Zongsoft.Data.Range<DateTime>?, Zongsoft.Data.ConditionOperator?[]>);
			Assert.Same(tupleType, TypeExtension.GetType(tupleType.FullName));
			Assert.Same(tupleType, TypeExtension.GetType("ValueTuple<string, date? ,binary , guid?[], RANGE<datetime>?, Zongsoft.Data.ConditionOperator? [ ]@Zongsoft.Core >"));
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
