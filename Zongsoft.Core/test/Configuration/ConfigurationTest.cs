using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Configuration
{
	public class ConfigurationTest
	{
		[Fact]
		public void TestResolveModels()
		{
			var configuration = Models.ModelConfigurationTest.GetConfiguration();

			Assert.NotNull(configuration);
			Assert.NotEmpty(configuration.Providers);

			TestGeneral(configuration.GetOption<General>("/general"));
			TestMobile(configuration.GetOption<Mobile>("mobile"));
		}

		[Fact]
		public void TestResolveXml()
		{
			var configuration = Xml.XmlConfigurationTest.GetConfiguration();

			Assert.NotNull(configuration);
			Assert.NotEmpty(configuration.Providers);

			TestGeneral(configuration.GetOption<General>("/general"));
			TestMobile(configuration.GetOption<Mobile>("mobile"));
		}

		[Fact]
		public void TestResolveConnectionSettings()
		{
			var configuration = Xml.XmlConfigurationTest.GetConfiguration();

			Assert.NotNull(configuration);
			Assert.NotEmpty(configuration.Providers);

			var settings = configuration.GetOption<ConnectionSettingCollection>("/data/connectionSettings");

			Assert.NotNull(settings);
			Assert.NotEmpty(settings);

			var setting = settings.Get("db1");

			Assert.NotNull(setting);
			Assert.Equal("db1", setting.Name);
			Assert.Equal("mysql", setting.Driver);
			Assert.Equal("db1.connectionString", setting.Value);

			Assert.True(setting.HasProperties);
			Assert.Equal(2, setting.Properties.Count);

			Assert.True(setting.Properties.TryGetValue("driver", out var value));
			Assert.Equal("mysql", value);
			Assert.True(setting.Properties.TryGetValue("mode", out value));
			Assert.Equal("all", value);

			settings = configuration.GetOption<ConnectionSettingCollection>("/externals/redis/connectionSettings");

			Assert.NotNull(settings);
			Assert.NotEmpty(settings);

			setting = settings.Get("redis");

			Assert.NotNull(setting);
			Assert.Equal("redis", setting.Name);
			Assert.Equal("redis.connectionString", setting.Value);

			Assert.False(setting.HasProperties);
			Assert.Empty(setting.Properties);
		}

		[Fact]
		public void TestAttachXml()
		{
			var configuration = Xml.XmlConfigurationTest.GetConfiguration();

			Assert.NotNull(configuration);
			Assert.NotEmpty(configuration.Providers);

			var general = configuration.GetOption<General>("general");

			Assert.NotNull(general);

			general.Name = "newname";
			general.IsIntranet = false;
			general.Certificates.Default = "test";

			configuration.SetOption(general, "general");

			Assert.Equal("newname", configuration.GetSection("general:name").Value);
			Assert.Equal("False", configuration.GetSection("general:intranet").Value);
			//Assert.Equal("test", configuration.GetSection("general:certificates:default").Value);
		}

		#region 私有方法
		private void TestGeneral(General general)
		{
			Assert.NotNull(general);
			Assert.Equal("general.name", general.Name);
			Assert.True(general.IsIntranet);

			Assert.NotNull(general.Certificates);
			Assert.NotEmpty(general.Certificates);
			Assert.Equal(2, general.Certificates.Count);

			Assert.Equal("main", general.Certificates.Default);
			Assert.NotNull(general.Certificates.GetDefault());
			Assert.Same(general.Certificates.GetDefault(), general.Certificates[general.Certificates.Default]);

			var certificate = general.Certificates["main"];
			Assert.NotNull(certificate);
			Assert.Equal("main", certificate.Name);
			Assert.Equal("C001", certificate.Code);
			Assert.Equal("xxxx", certificate.Secret);

			certificate = general.Certificates["test"];
			Assert.NotNull(certificate);
			Assert.Equal("test", certificate.Name);
			Assert.Equal("C002", certificate.Code);
			Assert.Equal("zzzz", certificate.Secret);
		}

		private void TestMobile(Mobile mobile)
		{
			Assert.NotNull(mobile);
			Assert.Equal("Shenzhen", mobile.Region);
			Assert.True(string.IsNullOrEmpty(mobile.Certificate));

			Assert.NotEmpty(mobile.Messages);
			Assert.Equal(1, mobile.Messages.Count);

			var message = mobile.Messages["alarm"];
			Assert.NotNull(message);
			Assert.Equal("Alarm", message.Name);
			Assert.Equal("SMS_01", message.Code);
			Assert.Equal("Zongsoft", message.Scheme);

			Assert.NotEmpty(mobile.Voices);
			Assert.Equal(2, mobile.Voices.Count);
			Assert.NotEmpty(mobile.Voices.Numbers);
			Assert.Equal(2, mobile.Voices.Numbers.Length);

			var voice = mobile.Voices["Alarm"];
			Assert.NotNull(voice);
			Assert.Equal("Alarm", voice.Name);
			Assert.Equal("TTS_01", voice.Code);

			voice = mobile.Voices["Password.forget"];
			Assert.NotNull(voice);
			Assert.Equal("Password.Forget", voice.Name);
			Assert.Equal("TTS_02", voice.Code);

			Assert.NotEmpty(mobile.Notifications);

			var notification = mobile.Notifications["Wechat"];
			Assert.Equal("wechat", notification.Key);
			Assert.Equal("A123", notification.Code);
			Assert.Equal("****", notification.Secret);
		}
		#endregion
	}

	#region 选项实体
	public class General
	{
		public General()
		{
			this.Certificates = new CertificateCollection();
		}

		public string Name
		{
			get; set;
		}

		[ConfigurationProperty("Intranet")]
		public bool IsIntranet
		{
			get; set;
		}

		public CertificateCollection Certificates
		{
			get;
		}
	}

	public class Certificate
	{
		public string Name
		{
			get; set;
		}

		public string Code
		{
			get; set;
		}

		public string Secret
		{
			get; set;
		}
	}

	public class CertificateCollection : Zongsoft.Collections.NamedCollectionBase<Certificate>
	{
		public string Default
		{
			get; set;
		}

		public Certificate GetDefault()
		{
			var name = this.Default;

			if(!string.IsNullOrEmpty(name) && this.TryGet(name, out var certificate))
				return certificate;

			return null;
		}

		protected override string GetKeyForItem(Certificate item)
		{
			return item.Name;
		}
	}

	public class Mobile
	{
		public string Region
		{
			get; set;
		}

		public string Certificate
		{
			get; set;
		}

		public IDictionary<string, Template> Messages
		{
			get; set;
		}

		public Voices Voices
		{
			get; set;
		}

		[ConfigurationProperty("Pushing")]
		public Notifications Notifications
		{
			get; set;
		}
	}

	public class Voices : Dictionary<string, Template>
	{
		public Voices() : base(StringComparer.OrdinalIgnoreCase)
		{
		}

		[TypeConverter(typeof(NumbersConverter))]
		public string[] Numbers
		{
			get; set;
		}

		public class NumbersConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				if(sourceType == typeof(string))
					return true;

				return base.CanConvertFrom(context, sourceType);
			}

			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
			{
				if(destinationType == typeof(string))
					return true;

				return base.CanConvertTo(context, destinationType);
			}

			public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
			{
				if(value is string text)
					return Common.StringExtension.Slice(text, ',').ToArray();

				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
			{
				if(value is string[] array)
					return string.Join(',', array);

				return base.ConvertTo(context, culture, value, destinationType);
			}
		}
	}

	public class Notifications : Zongsoft.Collections.NamedCollectionBase<App>
	{
		protected override string GetKeyForItem(App item)
		{
			return item.Key;
		}
	}

	public class Template
	{
		public string Name
		{
			get; set;
		}

		public string Code
		{
			get; set;
		}

		public string Scheme
		{
			get; set;
		}
	}

	public class App
	{
		public string Key
		{
			get; set;
		}

		public string Code
		{
			get; set;
		}

		public string Secret
		{
			get; set;
		}
	}
	#endregion
}
