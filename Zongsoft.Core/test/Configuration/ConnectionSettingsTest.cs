﻿using System;
using System.Net;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Configuration.Tests;

public class ConnectionSettingsTest
{
	private static readonly DateTime DATE = new(1979, 5, 15);
	private static readonly string ConnectionString = $" ;; server=192.168.0.1:8080, localhost:8088  ; Integer=100 ; enabled ; double= 1.23; ;  boolean= true ; text= MyString; dateTime={DATE:yyyy-M-d}; ;";

	[Fact]
	public void TestConnectionDescriptor()
	{
		var descriptors = MyDriver.Instance.Descriptors;
		Assert.NotNull(descriptors);
		Assert.NotEmpty(descriptors);
		Assert.Equal(14, descriptors.Count);

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
		Assert.NotEmpty(settings);
		Assert.NotNull(settings.Value);
		Assert.NotEmpty(settings.Value);
		Assert.Equal(7, settings.Count());

		Assert.Equal(100, settings.GetValue<int>("integer"));
		Assert.Equal(1.23, settings.GetValue<double>("double"));
		Assert.True(settings.GetValue<bool>("boolean"));
		Assert.Equal("MyString", settings.GetValue<string>("Text"));
		Assert.Equal(DATE, settings.GetValue<DateTime>("DateTime"));

		Assert.Null(settings.GetValue<bool?>("enabled"));
		Assert.True(settings.SetValue("enabled", true));
		Assert.True(settings.GetValue<bool>("enabled"));
		Assert.True(settings.SetValue("enabled", string.Empty));
		Assert.Equal(6, settings.Count());

		Assert.True(settings.SetValue("NewKey", "NewValue"));
		Assert.Equal("NewValue", settings.GetValue<string>("NewKey"));

		settings.Port = 999;
		Assert.Equal(999, settings.Port);

		Assert.NotNull(settings.Value);
		Assert.NotEmpty(settings.Value);
	}

	[Fact]
	public void TestConnectionSettings()
	{
		var settings = MyDriver.Instance.GetSettings(ConnectionString);
		Assert.NotNull(settings);
		Assert.NotNull(settings.Value);
		Assert.NotEmpty(settings.Value);
		Assert.True(settings.IsDriver(MyDriver.Instance));
		Assert.Same(MyDriver.Instance, settings.Driver);

		Assert.True(settings.Boolean);
		Assert.Equal(7969, settings.Port);
		Assert.Equal(100, settings.Integer);
		Assert.Equal(1.23, settings.Double);
		Assert.Equal("MyString", settings.Text, true);
		Assert.Equal(DATE, settings.Birthday);
		Assert.Equal(DateTime.Today.Year - DATE.Year, settings.Age);

		Assert.NotNull(settings.Server);
		Assert.NotEmpty(settings.Server);
		Assert.Equal(2, settings.Server.Length);
		Assert.IsType<IPEndPoint>(settings.Server[0]);
		Assert.Equal(IPEndPoint.Parse("192.168.0.1:8080"), settings.Server[0]);
		Assert.IsType<DnsEndPoint>(settings.Server[1]);
		Assert.Equal(new DnsEndPoint("localhost", 8088), settings.Server[1]);

		settings.Server = [.. settings.Server, IPEndPoint.Parse("127.0.0.1:88")];
		Assert.Equal(3, settings.Server.Length);
		Assert.IsType<IPEndPoint>(settings.Server[0]);
		Assert.Equal(IPEndPoint.Parse("192.168.0.1:8080"), settings.Server[0]);
		Assert.IsType<DnsEndPoint>(settings.Server[1]);
		Assert.Equal(new DnsEndPoint("localhost", 8088), settings.Server[1]);
		Assert.IsType<IPEndPoint>(settings.Server[2]);
		Assert.Equal(IPEndPoint.Parse("127.0.0.1:88"), settings.Server[2]);

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

		var date = DateTime.Today.AddDays(3);
		settings.Birthday = date;
		Assert.Equal(date, settings.Birthday);
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

		var seconds = 15;
		settings.DurationSeconds = TimeSpan.FromSeconds(seconds);
		Assert.Equal(TimeSpan.FromSeconds(seconds), settings.DurationSeconds);

		options = settings.GetOptions();
		Assert.NotNull(options);
		Assert.Equal(date, options.DateTime);
		Assert.Equal(timeout, options.ConnectionTimeout);
		Assert.Equal(timeout, options.ExecutionTimeout);
		Assert.Equal(seconds, options.Seconds);

		options = ConnectionSettingsUtility.GetOptions<MyConnectionOptions>(settings);
		Assert.NotNull(options);
		Assert.Equal(date, options.DateTime);
		Assert.Equal(timeout, options.ConnectionTimeout);
		Assert.Equal(timeout, options.ExecutionTimeout);
		Assert.Equal(seconds, options.Seconds);
	}

	[Fact]
	public void TestGetSettings()
	{
		Assert.True(ConnectionSettings.Drivers.TryGetValue(MyDriver.NAME, out var driver));
		Assert.NotNull(driver);

		var text = "server=127.0.0.1:80";
		var settings = driver.GetSettings(text);
		Assert.NotNull(settings);
		Assert.NotEmpty(settings);

		var options = settings.GetOptions<MyConnectionOptions>();
		Assert.NotNull(options);
		Assert.NotNull(options.Server);
		Assert.NotEmpty(options.Server);
		Assert.Single(options.Server);
		Assert.Equal("127.0.0.1:80", options.Server.First().ToString());

		text = "server=127.0.0.1:88";
		settings = driver.GetSettings(text);
		Assert.NotNull(settings);
		Assert.NotEmpty(settings);

		options = settings.GetOptions<MyConnectionOptions>();
		Assert.NotNull(options);
		Assert.NotNull(options.Server);
		Assert.NotEmpty(options.Server);
		Assert.Single(options.Server);
		Assert.Equal("127.0.0.1:88", options.Server.First().ToString());
	}

	[Fact]
	public void TestGetSettings1()
	{
		var settings1 = MyDriver.Instance.GetSettings(ConnectionString);
		Assert.NotNull(settings1);
		Assert.NotEmpty(settings1);
		Assert.Empty(settings1.Name);

		var settings2 = ConnectionSettingsUtility.GetSettings(MyDriver.Instance, ConnectionString);
		Assert.NotNull(settings2);
		Assert.NotEmpty(settings2);
		Assert.Empty(settings2.Name);
		Assert.IsType<MyConnectionSettings>(settings2);

		var mySettings = (MyConnectionSettings)settings2;
		Assert.Equal(settings1.Name, settings2.Name);
		Assert.Equal(settings1.Value, settings2.Value);
		Assert.Equal(settings1.UserName, mySettings.UserName);
		Assert.Equal(settings1.Password, mySettings.Password);
		Assert.Equal(settings1.CertificateFile, mySettings.CertificateFile);
		Assert.Equal(settings1.CertificateSecret, mySettings.CertificateSecret);
		Assert.Equal(settings1.Server, mySettings.Server);
		Assert.Equal(settings1.Timeout, mySettings.Timeout);
		Assert.Equal(settings1.Port, mySettings.Port);
		Assert.Equal(settings1.Text, mySettings.Text);
		Assert.Equal(settings1.Double, mySettings.Double);
		Assert.Equal(settings1.Integer, mySettings.Integer);
		Assert.Equal(settings1.Boolean, mySettings.Boolean);
		Assert.Equal(settings1.Birthday, mySettings.Birthday);
		Assert.Equal(settings1.DurationSeconds, mySettings.DurationSeconds);
	}

	[Fact]
	public void TestGetSettings2()
	{
		const string NAME = "MySettings";

		var settings1 = MyDriver.Instance.GetSettings(NAME, ConnectionString);
		Assert.NotNull(settings1);
		Assert.NotEmpty(settings1);
		Assert.Equal(NAME, settings1.Name);

		var settings2 = ConnectionSettingsUtility.GetSettings(MyDriver.Instance, NAME, ConnectionString);
		Assert.NotNull(settings2);
		Assert.NotEmpty(settings2);
		Assert.Equal(NAME, settings2.Name);
		Assert.IsType<MyConnectionSettings>(settings2);

		var mySettings = (MyConnectionSettings)settings2;
		Assert.Equal(settings1.Name, settings2.Name);
		Assert.Equal(settings1.Value, settings2.Value);
		Assert.Equal(settings1.UserName, mySettings.UserName);
		Assert.Equal(settings1.Password, mySettings.Password);
		Assert.Equal(settings1.CertificateFile, mySettings.CertificateFile);
		Assert.Equal(settings1.CertificateSecret, mySettings.CertificateSecret);
		Assert.Equal(settings1.Server, mySettings.Server);
		Assert.Equal(settings1.Timeout, mySettings.Timeout);
		Assert.Equal(settings1.Port, mySettings.Port);
		Assert.Equal(settings1.Text, mySettings.Text);
		Assert.Equal(settings1.Double, mySettings.Double);
		Assert.Equal(settings1.Integer, mySettings.Integer);
		Assert.Equal(settings1.Boolean, mySettings.Boolean);
		Assert.Equal(settings1.Birthday, mySettings.Birthday);
		Assert.Equal(settings1.DurationSeconds, mySettings.DurationSeconds);
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
		private MyDriver() : base(NAME) { }
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

		public EndPoint[] Server
		{
			get => this.GetValue<EndPoint[]>();
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

		[Alias(nameof(MyConnectionOptions.Seconds))]
		[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
		public TimeSpan DurationSeconds
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

		[Alias(nameof(MyConnectionOptions.DateTime))]
		public DateTime Birthday
		{
			get => this.GetValue<DateTime>();
			set => this.SetValue(value);
		}

		public short Age => (short)(DateTime.Today.Year - this.Birthday.Year);
	}

	public class MyConnectionOptions
	{
		public ICollection<EndPoint> Server { get; set; }
		public DateTime DateTime { get; set; }
		public TimeSpan ConnectionTimeout { get; set; }
		public TimeSpan ExecutionTimeout { get; set; }
		public int Seconds { get; set; }
	}

	public enum AuthenticationMode
	{
		None,
		User,
		Certificate,
	}
}
