using System;
using System.ComponentModel;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Configuration;

public class ConnectionSettingsTest
{
	private static readonly DateTime DATE = new(1979, 5, 15);
	private static readonly string ConnectionString = $" ;;  ; Integer=100 ; enabled ; double= 1.23; ;  boolean= true ; text= MyString; dateTime={DATE:yyyy-M-d}; ;";

	[Fact]
	public void TestDescriptorCollection()
	{
		var collection = new ConnectionSettingDescriptorCollection();
		Assert.Empty(collection);

		collection.Add("P1", typeof(int), 100);
		Assert.Single(collection);

		collection.Add("P2", ["A2"], typeof(string));
		Assert.Equal(2, collection.Count);

		Assert.True(collection.Contains("P1"));
		Assert.False(collection.Contains("A1"));
		Assert.True(collection.Contains("P2"));
		Assert.True(collection.Contains("A2"));

		Assert.True(collection.TryGetValue("P1", out var descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P1", descriptor.Name);
		Assert.Null(descriptor.Aliases);
		Assert.Equal(typeof(int), descriptor.Type);
		Assert.Equal(100, descriptor.DefaultValue);

		Assert.True(collection.TryGetValue("P2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));
		Assert.Equal(typeof(string), descriptor.Type);
		Assert.Null(descriptor.DefaultValue);

		Assert.True(collection.TryGetValue("A2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));
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
		Assert.True(descriptor.HasAlias("A2"));

		Assert.True(collection.TryGetValue("A2", out descriptor));
		Assert.NotNull(descriptor);
		Assert.Equal("P2", descriptor.Name);
		Assert.True(descriptor.HasAlias("A2"));

		collection.Remove("A2");
		Assert.Empty(collection);
	}

	[Fact]
	public void TestConnectionDescriptor()
	{
		var descriptors = MyDriver.Descriptors;
		Assert.NotNull(descriptors);
		Assert.NotEmpty(descriptors);
		Assert.Equal(12, descriptors.Count);

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
	public void TestConnectionSettingsGetValueAndSetValue()
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
	public void TestConnectionSettings()
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
		Assert.Equal(DateTime.Today.Year - DATE.Year, settings.Age);

		settings.Port = 996;
		Assert.Equal(996, settings.Port);
		settings.Timeout = TimeSpan.FromSeconds(99);
		Assert.Equal(TimeSpan.FromSeconds(99), settings.Timeout);
		settings.Text = null;
		Assert.Null(settings.Text);
		settings.Text = string.Empty;
		Assert.Null(settings.Text);
		settings.Text = nameof(settings.Text);
		Assert.Equal(nameof(settings.Text), settings.Text);
		settings.UserName = "admin";
		Assert.Equal("admin", settings.UserName);
		settings.Password = "password";
		Assert.Equal("password", settings.Password);
	}

	[Fact]
	public void TestConnectionSettingsGetOptions()
	{
		var TIMEOUT = TimeSpan.FromMinutes(1);

		var settings = MyDriver.Instance.GetSettings(ConnectionString);
		Assert.NotNull(settings);
		Assert.Equal(DATE, settings.Birthday);
		Assert.Equal(TIMEOUT, settings.Timeout);

		var options = settings.GetOptions();
		Assert.NotNull(options);
		Assert.Equal(DATE, options.DateTime);
		Assert.Equal(TIMEOUT, options.ConnectionTimeout);
		Assert.Equal(TIMEOUT, options.ExecutionTimeout);

		var date = DateTime.Today;
		var timeout = TimeSpan.Parse("1:2:3");

		settings.Birthday = date;
		settings.Timeout = timeout;
		Assert.Equal(date, settings.Birthday);
		Assert.Equal(timeout, settings.Timeout);

		options = settings.GetOptions();
		Assert.NotNull(options);
		Assert.Equal(date, options.DateTime);
		Assert.Equal(timeout, options.ConnectionTimeout);
		Assert.Equal(timeout, options.ExecutionTimeout);
	}

	public sealed class MyDriver : ConnectionSettingsDriver<MyConnectionSettings>
	{
		#region 常量定义
		internal const string NAME = "MyDriver";
		#endregion

		#region 单例字段
		public static readonly MyDriver Instance = new();
		#endregion

		#region 私有构造
		private MyDriver() : base(NAME)
		{
			this.Mapper = new MyMapper(this);
			this.Populator = new MyPopulator(this);
		}
		#endregion

		#region 嵌套子类
		private sealed class MyMapper(MyDriver driver) : MapperBase(driver) { }
		private sealed class MyPopulator(MyDriver driver) : PopulatorBase(driver) { }
		#endregion
	}

	public class MyConnectionSettings : ConnectionSettingsBase<MyDriver, MyConnectionOptions>
	{
		public MyConnectionSettings(MyDriver driver, string settings) : base(driver, settings) { }
		public MyConnectionSettings(MyDriver driver, string name, string settings) : base(driver, name, settings) { }

		public AuthenticationMode AuthenticationMode
		{
			get => this.GetValue<AuthenticationMode>();
			set => this.SetValue(value);
		}

		[ConnectionSetting($"{nameof(AuthenticationMode)}:{nameof(AuthenticationMode.User)}")]
		public string UserName
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[ConnectionSetting($"{nameof(AuthenticationMode)}:{nameof(AuthenticationMode.User)}")]
		public string Password
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[ConnectionSetting($"{nameof(AuthenticationMode)}={nameof(AuthenticationMode.Certificate)}")]
		public string CertificateFile
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[ConnectionSetting($"{nameof(AuthenticationMode)}={nameof(AuthenticationMode.Certificate)}")]
		public string CertificateSecret
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[DefaultValue("1m")]
		[Alias(nameof(MyConnectionOptions.ConnectionTimeout))]
		[Alias(nameof(MyConnectionOptions.ExecutionTimeout))]
		public TimeSpan Timeout
		{
			get => this.GetValue<TimeSpan>();
			set => this.SetValue(value);
		}

		[DefaultValue(7969)]
		public ushort Port
		{
			get => this.GetValue<ushort>();
			set => this.SetValue(value);
		}

		public int Integer
		{
			get => this.GetValue<int>();
			set => this.SetValue(value);
		}

		public double Double
		{
			get => this.GetValue<double>();
			set => this.SetValue(value);
		}

		public bool Boolean
		{
			get => this.GetValue<bool>();
			set => this.SetValue(value);
		}

		[ConnectionSetting(true, Format = "MyFormat")]
		public string Text
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[Alias("DateTime")]
		public DateTime Birthday
		{
			get => this.GetValue<DateTime>();
			set => this.SetValue(value);
		}

		public short Age => (short)(DateTime.Today.Year - this.Birthday.Year);
	}

	public class MyConnectionOptions
	{
		public DateTime DateTime { get; set; }
		public TimeSpan ConnectionTimeout { get; set; }
		public TimeSpan ExecutionTimeout { get; set; }
	}

	public enum AuthenticationMode
	{
		None,
		User,
		Certificate,
	}
}