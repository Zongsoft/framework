using System;
using System.Reflection;

using Xunit;

namespace Zongsoft.Reflection.Tests
{
	public class ReflectorTest
	{
		[Fact]
		public void TestValueType()
		{
			var target = new MyValue(5);

			Assert.Equal(100, (int)Reflector.GetValue(ref target, nameof(MyValue.IntegerField)));
			Assert.Equal(123.05m, (decimal)Reflector.GetValue(ref target, nameof(MyValue.DecimalProperty)));
			Assert.Equal("StringField", (string)Reflector.GetValue(ref target, nameof(MyValue.StringField)));
			Assert.Equal("StringProperty", (string)Reflector.GetValue(ref target, nameof(MyValue.StringProperty)));

			Assert.Equal("#0", Reflector.GetValue(ref target, "Item", 0));
			Assert.Equal("#1", Reflector.GetValue(ref target, "Item", 1));
			Assert.Equal("#2", Reflector.GetValue(ref target, "Item", 2));
			Assert.Equal("#3", Reflector.GetValue(ref target, "Item", 3));
			Assert.Equal("#4", Reflector.GetValue(ref target, "Item", 4));
		}

		[Fact]
		public void TestClassType()
		{
			var target = new MyClass(5);

			Assert.Equal(100, (int)Reflector.GetValue(ref target, nameof(MyValue.IntegerField)));
			Assert.Equal(123.05m, (decimal)Reflector.GetValue(ref target, nameof(MyValue.DecimalProperty)));
			Assert.Equal("StringField", (string)Reflector.GetValue(ref target, nameof(MyValue.StringField)));
			Assert.Equal("StringProperty", (string)Reflector.GetValue(ref target, nameof(MyValue.StringProperty)));

			Assert.Equal("#0", Reflector.GetValue(ref target, "Item", 0));
			Assert.Equal("#1", Reflector.GetValue(ref target, "Item", 1));
			Assert.Equal("#2", Reflector.GetValue(ref target, "Item", 2));
			Assert.Equal("#3", Reflector.GetValue(ref target, "Item", 3));
			Assert.Equal("#4", Reflector.GetValue(ref target, "Item", 4));
		}

		private class MyClass
		{
			#region 私有变量
			private readonly string[] _items;
			#endregion

			#region 公共字段
			public int IntegerField;
			public string StringField;
			#endregion

			#region 构造函数
			public MyClass(int count)
			{
				_items = new string[count];

				for(int i = 0; i < count; i++)
				{
					_items[i] = "#" + i.ToString();
				}

				this.IntegerField = 100;
				this.StringField = "StringField";
				this.DecimalProperty = 123.05m;
				this.StringProperty = "StringProperty";
			}
			#endregion

			#region 公共属性
			public decimal DecimalProperty
			{
				get; set;
			}

			public string StringProperty
			{
				get; set;
			}

			public string this[int index]
			{
				get => _items[index];
				set => _items[index] = value;
			}
			#endregion
		}

		private struct MyValue
		{
			#region 私有变量
			private readonly string[] _items;
			#endregion

			#region 公共字段
			public int IntegerField;
			public string StringField;
			#endregion

			#region 构造函数
			public MyValue(int count)
			{
				_items = new string[count];

				for(int i = 0; i < count; i++)
				{
					_items[i] = "#" + i.ToString();
				}

				this.IntegerField = 100;
				this.StringField = "StringField";
				this.DecimalProperty = 123.05m;
				this.StringProperty = "StringProperty";
			}
			#endregion

			#region 公共属性
			public decimal DecimalProperty
			{
				get; set;
			}

			public string StringProperty
			{
				get; set;
			}

			public string this[int index]
			{
				get => _items[index];
				set => _items[index] = value;
			}
			#endregion
		}
	}
}
