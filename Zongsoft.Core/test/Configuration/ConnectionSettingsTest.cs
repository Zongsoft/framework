using System;
using System.ComponentModel;

using Xunit;

using Zongsoft.ComponentModel;

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
	public void TestConnectionDescriptor()
	{
		var descriptors = MyDriver.Descriptors;
		Assert.NotNull(descriptors);
		Assert.NotEmpty(descriptors);
		Assert.Equal(11, descriptors.Count);

		ConnectionSettingDescriptor descriptor = null;
		Assert.True(descriptors.TryGetValue(nameof(MyConnectionSettings.Port), out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(ushort), descriptor.Type);
		Assert.Equal((ushort)7969, descriptor.DefaultValue);

		Assert.True(descriptors.TryGetValue(nameof(MyConnectionSettings.Birthday), out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(DateTime), descriptor.Type);
		Assert.True(descriptors.TryGetValue("DateTime", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(DateTime), descriptor.Type);

		Assert.True(descriptors.TryGetValue(nameof(MyConnectionSettings.Text), out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(string), descriptor.Type);
		Assert.True(descriptor.Required);
		Assert.Equal("MyFormat", descriptor.Format);

		Assert.True(descriptors.TryGetValue(nameof(MyConnectionSettings.AuthenticationMode), out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(AuthenticationMode), descriptor.Type);
		Assert.False(descriptor.Required);
		Assert.Equal(AuthenticationMode.None, descriptor.DefaultValue);
		Assert.Empty(descriptor.Dependencies);
		Assert.NotEmpty(descriptor.Options);
		Assert.Equal(3, descriptor.Options.Count);
		Assert.Equal(nameof(AuthenticationMode.None), descriptor.Options[0].Name);
		Assert.Equal(nameof(AuthenticationMode.User), descriptor.Options[1].Name);
		Assert.Equal(nameof(AuthenticationMode.Certificate), descriptor.Options[2].Name);

		Assert.True(descriptors.TryGetValue(nameof(MyConnectionSettings.UserName), out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(string), descriptor.Type);
		Assert.False(descriptor.Required);
		Assert.Null(descriptor.DefaultValue);
		Assert.Empty(descriptor.Options);
		Assert.NotEmpty(descriptor.Dependencies);
		Assert.Single(descriptor.Dependencies);
		Assert.Equal(nameof(MyConnectionSettings.AuthenticationMode), descriptor.Dependencies[0].Name);
		Assert.Equal(nameof(AuthenticationMode.User), descriptor.Dependencies[0].Value);

		Assert.True(descriptors.TryGetValue(nameof(MyConnectionSettings.CertificateFile), out descriptor));
		Assert.NotNull(descriptor);
		Assert.Same(typeof(string), descriptor.Type);
		Assert.False(descriptor.Required);
		Assert.Null(descriptor.DefaultValue);
		Assert.Empty(descriptor.Options);
		Assert.NotEmpty(descriptor.Dependencies);
		Assert.Single(descriptor.Dependencies);
		Assert.Equal(nameof(MyConnectionSettings.AuthenticationMode), descriptor.Dependencies[0].Name);
		Assert.Equal(nameof(AuthenticationMode.Certificate), descriptor.Dependencies[0].Value);
	}

	[Fact]
	public void TestConnectionSettings()
	{
		var settings = new ConnectionSettings("MyConnectionSettings", ConnectionString);
		Assert.Equal(5, settings.Entries.Count);

		Assert.False(settings.Entries.ContainsKey("enabled"));
		Assert.True(settings.SetValue("enabled", true));
		Assert.Equal(6, settings.Entries.Count);
		Assert.True(settings.Entries.ContainsKey("enabled"));

		Assert.True(settings.SetValue("enabled", string.Empty));
		Assert.False(settings.Entries.ContainsKey("enabled"));
		Assert.Equal(5, settings.Entries.Count);

		Assert.True(settings.Entries.TryGetValue("integer", out var text));
		Assert.Equal("100", text, true);
		Assert.True(settings.Entries.TryGetValue("double", out text));
		Assert.Equal("1.23", text, true);
		Assert.True(settings.Entries.TryGetValue("boolean", out text));
		Assert.Equal("true", text, true);
		Assert.True(settings.Entries.TryGetValue("text", out text));
		Assert.Equal("MyString", text, true);
		Assert.True(settings.Entries.TryGetValue("DateTime", out text));
		Assert.Equal(DATE.ToString("yyyy-M-d"), text, true);

		Assert.True(settings.SetValue("NewKey", "NewValue"));
		Assert.Equal("NewValue", settings.GetValue<string>("NewKey"));

		settings.Port = 999;
		Assert.Equal(999, settings.Port);
	}

	[Fact]
	public void TestGetOptions()
	{
		var settings = MyDriver.Instance.GetSettings(ConnectionString);
		Assert.NotNull(settings);
		Assert.True(settings.IsDriver(MyDriver.Instance));
		Assert.Same(MyDriver.Instance, settings.Driver);

		Assert.True(settings.Boolean);
		Assert.Equal(7969, settings.Port);
		Assert.Equal(100, settings.Integer);
		Assert.Equal(1.23, settings.Double);
		Assert.Equal("MyString", settings.Text, true);
		Assert.Equal(DATE, settings.Birthday);

		settings.Port = 996;
		Assert.Equal(996, settings.Port);

		var options = MyDriver.Instance.GetSettings(ConnectionString);
		Assert.NotNull(options);
		Assert.True(options.Boolean);
		Assert.Equal(100, options.Integer);
		Assert.Equal(1.23, options.Double);
		Assert.Equal("MyString", options.Text, true);
		Assert.Equal(DATE, options.Birthday);
		Assert.Equal(DateTime.Today.Year - DATE.Year, options.Age);
	}

	public sealed class MyDriver : ConnectionSettingsDriver<MyConnectionSettings>
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
			protected override bool OnPopulate(ref MyConnectionSettings options, ConnectionSettingDescriptor descriptor, object value)
			{
				if(descriptor.Equals(nameof(MyConnectionSettings.Birthday)) && Common.Convert.TryConvertValue<DateTime>(value, out var birthday))
					options.Age = (short)(DateTime.Today.Year - birthday.Year);

				return base.OnPopulate(ref options, descriptor, value);
			}
		}
		#endregion
	}

	public class MyConnectionSettings : ConnectionSettingsBase<MyDriver>
	{
		public MyConnectionSettings(MyDriver driver, string settings) : base(driver, settings) { }
		public MyConnectionSettings(MyDriver driver, string name, string settings) : base(driver, name, settings) { }

		public AuthenticationMode AuthenticationMode { get; set; }

		[ConnectionSetting($"{nameof(AuthenticationMode)}:{nameof(AuthenticationMode.User)}")]
		public string UserName { get; set; }
		[ConnectionSetting($"{nameof(AuthenticationMode)}:{nameof(AuthenticationMode.User)}")]
		public string Password { get; set; }
		[ConnectionSetting($"{nameof(AuthenticationMode)}={nameof(AuthenticationMode.Certificate)}")]
		public string CertificateFile { get; set; }
		[ConnectionSetting($"{nameof(AuthenticationMode)}={nameof(AuthenticationMode.Certificate)}")]
		public string CertificateSecret { get; set; }

		[DefaultValue(7969)]
		public ushort Port { get; set; }
		public int Integer { get; set; }
		public double Double { get; set; }
		public bool Boolean { get; set; }
		[ConnectionSetting(true, Format = "MyFormat")]
		public string Text { get; set; }
		[Alias("DateTime")]
		public DateTime Birthday { get; set; }
		public short Age { get; internal set; }
	}

	public enum AuthenticationMode
	{
		None,
		User,
		Certificate,
	}
}