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
			Assert.Single(genericTypes);
			Assert.Same(genericTypes[0], typeof(IService<Employee>));

			Assert.True(TypeExtension.IsAssignableFrom(typeof(PersonServiceBase<>), typeof(EmployeeService), out genericTypes));
			Assert.NotEmpty(genericTypes);
			Assert.Single(genericTypes);
			Assert.Same(genericTypes[0], typeof(PersonServiceBase<Employee>));
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
