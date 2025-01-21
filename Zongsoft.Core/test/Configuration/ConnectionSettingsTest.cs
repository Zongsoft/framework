using System;

using Xunit;

namespace Zongsoft.Configuration;

public class ConnectionSettingsTest
{
	private static readonly DateTime DATE = new DateTime(1979, 5, 15);
	private static readonly string ConnectionString = $" ;;  ; Integer=100 ; enabled ; double= 1.23; ;  boolean= true ; text= MyString; dateTime={DATE:yyyy-M-d}; ;";

	[Fact]
	public void TestDescriptorCollection()
	{
		var collection = new ConnectionSettingDescriptorCollection();
		Assert.Empty(collection);

		collection.Add("P1", typeof(int), 100);
		Assert.Single(collection);

		collection.Add("P2", "A2", typeof(string));
		Assert.Equal(2, collection.Count);

		Assert.True(collection.Contains("P1"));
		Assert.False(collection.Contains("A1"));
		Assert.True(collection.Contains("P2"));
		Assert.True(collection.Contains("A2"));

		Assert.True(collection.TryGetValue("P1", out var descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P1", descriptor.Name);
		Assert.Null(descriptor.Alias);
		Assert.Equal(typeof(int), descriptor.Type);
		Assert.Equal(100, descriptor.DefaultValue);

		Assert.True(collection.TryGetValue("P2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.Equal("A2", descriptor.Alias);
		Assert.Equal(typeof(string), descriptor.Type);
		Assert.Null(descriptor.DefaultValue);

		Assert.True(collection.TryGetValue("A2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.Equal("A2", descriptor.Alias);
		Assert.Equal(typeof(string), descriptor.Type);
		Assert.Null(descriptor.DefaultValue);

		collection.Remove("P1");
		Assert.Single(collection);

		Assert.False(collection.Contains("P1"));
		Assert.False(collection.Contains("A1"));
		Assert.True(collection.Contains("P2"));
		Assert.True(collection.Contains("A2"));

		Assert.True(collection.TryGetValue("P2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.Equal("A2", descriptor.Alias);

		Assert.True(collection.TryGetValue("A2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.Equal("A2", descriptor.Alias);

		collection.Remove("A2");
		Assert.Empty(collection);
	}

	[Fact]
	public void TestConnectionSettings()
	{
		var settings = new ConnectionSettings("MyConnectionSettings", ConnectionString);
		Assert.Equal(5, settings.Values.Count);

		Assert.False(settings.Values.ContainsKey("enabled"));
		Assert.True(settings.SetValue("enabled", true));
		Assert.Equal(6, settings.Values.Count);
		Assert.True(settings.Values.ContainsKey("enabled"));

		Assert.True(settings.SetValue("enabled", string.Empty));
		Assert.False(settings.Values.ContainsKey("enabled"));
		Assert.Equal(5, settings.Values.Count);

		Assert.True(settings.Values.TryGetValue("integer", out var text));
		Assert.Equal("100", text, true);
		Assert.True(settings.Values.TryGetValue("double", out text));
		Assert.Equal("1.23", text, true);
		Assert.True(settings.Values.TryGetValue("boolean", out text));
		Assert.Equal("true", text, true);
		Assert.True(settings.Values.TryGetValue("text", out text));
		Assert.Equal("MyString", text, true);
		Assert.True(settings.Values.TryGetValue("DateTime", out text));
		Assert.Equal(DATE.ToString("yyyy-M-d"), text, true);

		settings.Port = 999;
		Assert.Equal(999, settings.Port);
	}

	[Fact]
	public void TestGetOptions()
	{
		var settings = MyDriver.Instance.Create(ConnectionString);
		Assert.NotNull(settings);
		Assert.True(settings.IsDriver(MyDriver.Instance));
		Assert.Same(MyDriver.Instance, settings.Driver);

		settings.Port = 996;
		Assert.Equal(996, settings.Port);
		Assert.Equal(996, settings.GetValue<int>("Port"));

		Assert.True(settings.SetValue("Port", 999));
		Assert.Equal(999, settings.Port);
		Assert.Equal(999, settings.GetValue<int>("Port"));

		var options = MyDriver.Instance.GetOptions(settings);
		Assert.NotNull(options);
		Assert.True(options.Boolean);
		Assert.Equal(100, options.Integer);
		Assert.Equal(1.23, options.Double);
		Assert.Equal("MyString", options.Text, true);
		Assert.Equal(DATE, options.Birthday);
		Assert.Equal(DateTime.Today.Year - DATE.Year, options.Age);

		var target = settings.GetOptions();
		Assert.NotNull(target);
		Assert.IsType<MyOptions>(target);
		Assert.True(((MyOptions)target).Boolean);
		Assert.Equal(100, ((MyOptions)target).Integer);
		Assert.Equal(1.23, ((MyOptions)target).Double);
		Assert.Equal("MyString", ((MyOptions)target).Text, true);
		Assert.Equal(DATE, ((MyOptions)target).Birthday);
		Assert.Equal(DateTime.Today.Year - DATE.Year, ((MyOptions)target).Age);
	}

	public sealed class MyDriver : ConnectionSettingsDriver<MyOptions, MyDescriptorCollection>
	{
		#region 单例字段
		public static readonly MyDriver Instance = new();
		#endregion

		#region 私有构造
		private MyDriver() : base("MyDriver")
		{
			this.Mapper = new MyMapper(this);
			this.Populator = new MyPopulator(this);
		}
		#endregion

		#region 嵌套子类
		private sealed class MyMapper(MyDriver driver) : MapperBase(driver) { }
		private sealed class MyPopulator(MyDriver driver) : PopulatorBase(driver)
		{
			protected override bool OnPopulate(ref MyOptions model, ConnectionSettingDescriptor descriptor, object value)
			{
				if(MyDescriptorCollection.DateTime.Equals(descriptor.Name) && Common.Convert.TryConvertValue<DateTime>(value, out var birthday))
					model.Age = (short)(DateTime.Today.Year - birthday.Year);

				return base.OnPopulate(ref model, descriptor, value);
			}
		}
		#endregion
	}

	public sealed class MyDescriptorCollection : ConnectionSettingDescriptorCollection
	{
		public static readonly ConnectionSettingDescriptor Text = new(nameof(Text), typeof(string));
		public static readonly ConnectionSettingDescriptor Port = new(nameof(Port), nameof(Port), false, (ushort)7969);
		public static readonly ConnectionSettingDescriptor DateTime = new(nameof(DateTime), nameof(MyOptions.Birthday), typeof(DateTime));
		public static readonly ConnectionSettingDescriptor Integer = new(nameof(Integer), typeof(int));
		public static readonly ConnectionSettingDescriptor Boolean = new(nameof(Boolean), typeof(bool));
		public static readonly ConnectionSettingDescriptor Double = new(nameof(Double), typeof(double));

		public MyDescriptorCollection()
		{
			this.Add(Text);
			this.Add(Port);
			this.Add(DateTime);
			this.Add(Integer);
			this.Add(Boolean);
			this.Add(Double);
		}
	}

	public class MyOptions
	{
		public int Integer { get; set; }
		public double Double { get; set; }
		public bool Boolean { get; set; }
		public string Text { get; set; }
		public short Age { get; set; }
		public DateTime Birthday { get; set; }
	}
}