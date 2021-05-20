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
			var Today = DateTime.Today;
			var target = new MyValue(5);

			Assert.Equal(100, (int)Reflector.GetValue(ref target, nameof(MyValue.IntegerField)));
			Assert.Equal(123.05m, (decimal)Reflector.GetValue(ref target, nameof(MyValue.DecimalProperty)));
			Assert.Equal("StringField", (string)Reflector.GetValue(ref target, nameof(MyValue.StringField)));
			Assert.Equal("StringProperty", (string)Reflector.GetValue(ref target, nameof(MyValue.StringProperty)));
			Assert.Null(Reflector.GetValue(ref target, nameof(MyValue.NullableField)));
			Assert.Null(Reflector.GetValue(ref target, nameof(MyValue.NullableProperty)));

			Assert.Equal("#0", Reflector.GetValue(ref target, "Item", 0));
			Assert.Equal("#1", Reflector.GetValue(ref target, "Item", 1));
			Assert.Equal("#2", Reflector.GetValue(ref target, "Item", 2));
			Assert.Equal("#3", Reflector.GetValue(ref target, "Item", 3));
			Assert.Equal("#4", Reflector.GetValue(ref target, "Item", 4));

			Reflector.SetValue(ref target, nameof(MyValue.IntegerField), 200);
			Reflector.SetValue(ref target, nameof(MyValue.DecimalProperty), 456.78);
			Reflector.SetValue(ref target, nameof(MyValue.StringField), "NewStringField");
			Reflector.SetValue(ref target, nameof(MyValue.StringProperty), "NewStringProperty");

			Assert.Equal(200, (int)Reflector.GetValue(ref target, nameof(MyValue.IntegerField)));
			Assert.Equal(456.78m, (decimal)Reflector.GetValue(ref target, nameof(MyValue.DecimalProperty)));
			Assert.Equal("NewStringField", (string)Reflector.GetValue(ref target, nameof(MyValue.StringField)));
			Assert.Equal("NewStringProperty", (string)Reflector.GetValue(ref target, nameof(MyValue.StringProperty)));

			Reflector.SetValue(ref target, nameof(MyValue.NullableField), Today);
			Assert.Equal(Today, Reflector.GetValue(ref target, nameof(MyValue.NullableField)));
			Reflector.SetValue(ref target, nameof(MyValue.NullableProperty), Today);
			Assert.Equal(Today, Reflector.GetValue(ref target, nameof(MyValue.NullableProperty)));

			Reflector.SetValue(ref target, nameof(MyValue.NullableField), null);
			Assert.Null(Reflector.GetValue(ref target, nameof(MyValue.NullableField)));
			Reflector.SetValue(ref target, nameof(MyValue.NullableProperty), null);
			Assert.Null(Reflector.GetValue(ref target, nameof(MyValue.NullableProperty)));

			object obj = target;

			Reflector.SetValue(ref obj, nameof(MyValue.IntegerField), 300);
			Reflector.SetValue(ref obj, nameof(MyValue.DecimalProperty), 168.09);
			Reflector.SetValue(ref obj, nameof(MyValue.StringField), "MyStringField");
			Reflector.SetValue(ref obj, nameof(MyValue.StringProperty), "MyStringProperty");

			Assert.Equal(300, (int)Reflector.GetValue(ref obj, nameof(MyValue.IntegerField)));
			Assert.Equal(168.09m, (decimal)Reflector.GetValue(ref obj, nameof(MyValue.DecimalProperty)));
			Assert.Equal("MyStringField", (string)Reflector.GetValue(ref obj, nameof(MyValue.StringField)));
			Assert.Equal("MyStringProperty", (string)Reflector.GetValue(ref obj, nameof(MyValue.StringProperty)));

			Reflector.SetValue(ref obj, nameof(MyValue.NullableField), Today);
			Assert.Equal(Today, Reflector.GetValue(ref obj, nameof(MyValue.NullableField)));
			Reflector.SetValue(ref obj, nameof(MyValue.NullableProperty), Today);
			Assert.Equal(Today, Reflector.GetValue(ref obj, nameof(MyValue.NullableProperty)));

			Reflector.SetValue(ref obj, nameof(MyValue.NullableField), null);
			Assert.Null(Reflector.GetValue(ref obj, nameof(MyValue.NullableField)));
			Reflector.SetValue(ref obj, nameof(MyValue.NullableProperty), null);
			Assert.Null(Reflector.GetValue(ref obj, nameof(MyValue.NullableProperty)));
		}

		[Fact]
		public void TestClassType()
		{
			var Today = DateTime.Today;
			var target = new MyClass(5);

			Assert.Equal(100, (int)Reflector.GetValue(ref target, nameof(MyClass.IntegerField)));
			Assert.Equal(123.05m, (decimal)Reflector.GetValue(ref target, nameof(MyClass.DecimalProperty)));
			Assert.Equal("StringField", (string)Reflector.GetValue(ref target, nameof(MyClass.StringField)));
			Assert.Equal("StringProperty", (string)Reflector.GetValue(ref target, nameof(MyClass.StringProperty)));
			Assert.Null(Reflector.GetValue(ref target, nameof(MyClass.NullableField)));
			Assert.Null(Reflector.GetValue(ref target, nameof(MyClass.NullableProperty)));

			Assert.Equal("#0", Reflector.GetValue(ref target, "Item", 0));
			Assert.Equal("#1", Reflector.GetValue(ref target, "Item", 1));
			Assert.Equal("#2", Reflector.GetValue(ref target, "Item", 2));
			Assert.Equal("#3", Reflector.GetValue(ref target, "Item", 3));
			Assert.Equal("#4", Reflector.GetValue(ref target, "Item", 4));

			Reflector.SetValue(ref target, nameof(MyClass.IntegerField), 200);
			Reflector.SetValue(ref target, nameof(MyClass.DecimalProperty), 456.78);
			Reflector.SetValue(ref target, nameof(MyClass.StringField), "NewStringField");
			Reflector.SetValue(ref target, nameof(MyClass.StringProperty), "NewStringProperty");

			Assert.Equal(200, (int)Reflector.GetValue(ref target, nameof(MyClass.IntegerField)));
			Assert.Equal(456.78m, (decimal)Reflector.GetValue(ref target, nameof(MyClass.DecimalProperty)));
			Assert.Equal("NewStringField", (string)Reflector.GetValue(ref target, nameof(MyClass.StringField)));
			Assert.Equal("NewStringProperty", (string)Reflector.GetValue(ref target, nameof(MyClass.StringProperty)));

			Reflector.SetValue(ref target, nameof(MyClass.NullableField), Today);
			Assert.Equal(Today, Reflector.GetValue(ref target, nameof(MyClass.NullableField)));
			Reflector.SetValue(ref target, nameof(MyClass.NullableProperty), Today);
			Assert.Equal(Today, Reflector.GetValue(ref target, nameof(MyClass.NullableProperty)));

			Reflector.SetValue(ref target, nameof(MyClass.NullableField), null);
			Assert.Null(Reflector.GetValue(ref target, nameof(MyClass.NullableField)));
			Reflector.SetValue(ref target, nameof(MyClass.NullableProperty), null);
			Assert.Null(Reflector.GetValue(ref target, nameof(MyClass.NullableProperty)));

			object obj = target;

			Reflector.SetValue(ref obj, nameof(MyClass.IntegerField), 300);
			Reflector.SetValue(ref obj, nameof(MyClass.DecimalProperty), 168.09);
			Reflector.SetValue(ref obj, nameof(MyClass.StringField), "MyStringField");
			Reflector.SetValue(ref obj, nameof(MyClass.StringProperty), "MyStringProperty");

			Assert.Equal(300, (int)Reflector.GetValue(ref obj, nameof(MyClass.IntegerField)));
			Assert.Equal(168.09m, (decimal)Reflector.GetValue(ref obj, nameof(MyClass.DecimalProperty)));
			Assert.Equal("MyStringField", (string)Reflector.GetValue(ref obj, nameof(MyClass.StringField)));
			Assert.Equal("MyStringProperty", (string)Reflector.GetValue(ref obj, nameof(MyClass.StringProperty)));

			Reflector.SetValue(ref obj, nameof(MyClass.NullableField), Today);
			Assert.Equal(Today, Reflector.GetValue(ref obj, nameof(MyClass.NullableField)));
			Reflector.SetValue(ref obj, nameof(MyClass.NullableProperty), Today);
			Assert.Equal(Today, Reflector.GetValue(ref obj, nameof(MyClass.NullableProperty)));

			Reflector.SetValue(ref obj, nameof(MyClass.NullableField), null);
			Assert.Null(Reflector.GetValue(ref obj, nameof(MyClass.NullableField)));
			Reflector.SetValue(ref obj, nameof(MyClass.NullableProperty), null);
			Assert.Null(Reflector.GetValue(ref obj, nameof(MyClass.NullableProperty)));
		}

		private class MyClass
		{
			#region 私有变量
			private readonly string[] _items;
			#endregion

			#region 公共字段
			public int IntegerField;
			public string StringField;
			public DateTime? NullableField;
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
			public decimal DecimalProperty { get; set; }
			public string StringProperty { get; set; }
			public DateTime? NullableProperty { get; set; }

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
			public DateTime? NullableField;
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
				this.NullableField = null;
				this.NullableProperty = null;
			}
			#endregion

			#region 公共属性
			public decimal DecimalProperty { get; set; }
			public string StringProperty { get; set; }
			public DateTime? NullableProperty { get; set; }

			public string this[int index]
			{
				get => _items[index];
				set => _items[index] = value;
			}
			#endregion
		}
	}
}
