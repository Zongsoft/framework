using System;
using System.ComponentModel;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Configuration
{
	public class ConfigurationTest
	{
		[Fact]
		public void TestLoad()
		{
			var configuration = new ConfigurationBuilder()
				.AddOptionFile("Configuration/OptionConfigurationTest.option")
				.Build();

			Assert.NotEmpty(configuration.Providers);

			Assert.Equal("general.name", configuration.GetSection("general:name").Value);
			Assert.Equal("true", configuration.GetSection("general:internal").Value);
			Assert.Equal("main", configuration.GetSection("general:certificates:default").Value);

			Assert.Equal("main", configuration.GetSection("general:certificates:main:name").Value);
			Assert.Equal("C001", configuration.GetSection("general:certificates:main:code").Value);
			Assert.Equal("xxxx", configuration.GetSection("general:certificates:main:secret").Value);

			Assert.Equal("test", configuration.GetSection("general:certificates:test:name").Value);
			Assert.Equal("C002", configuration.GetSection("general:certificates:test:code").Value);
			Assert.Equal("zzzz", configuration.GetSection("general:certificates:test:secret").Value);

			Assert.Equal("Shenzhen", configuration.GetSection("mobile:region").Value);
			Assert.Null(configuration.GetSection("mobile.certificate").Value);

			Assert.Equal("Alarm", configuration.GetSection("mobile:messages:alarm:name").Value);
			Assert.Equal("SMS_01", configuration.GetSection("mobile:messages:alarm:code").Value);
			Assert.Equal("Zongsoft", configuration.GetSection("mobile:messages:alarm:scheme").Value);

			Assert.Equal("400123456, 400666888", configuration.GetSection("mobile:voices:numbers").Value);
			Assert.Equal("Alarm", configuration.GetSection("mobile:voices:alarm:name").Value);
			Assert.Equal("TTS_01", configuration.GetSection("mobile:voices:alarm:code").Value);
			Assert.Equal("Password.Forget", configuration.GetSection("mobile:voices:password.forget:name").Value);
			Assert.Equal("TTS_02", configuration.GetSection("mobile:voices:password.forget:code").Value);

			Assert.Equal("wechat", configuration.GetSection("mobile:pushing:wechat:key").Value);
			Assert.Equal("A123", configuration.GetSection("mobile:pushing:wechat:code").Value);
			Assert.Equal("****", configuration.GetSection("mobile:pushing:wechat:secret").Value);
		}

		[Fact]
		public void TestBind()
		{
			var configuration = new ConfigurationBuilder()
				.AddOptionFile("Configuration/OptionConfigurationTest.option")
				.Build();

			var general = configuration.GetOption<GeneralOptions>("/general");

			Assert.NotNull(general);
			Assert.Equal("general.name", general.Name);
			Assert.True(general.IsInternal);

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

			var mobile = configuration.GetOption<MobileOptions>("mobile");

			Assert.NotNull(mobile);
			Assert.Equal("Shenzhen", mobile.Region);
			Assert.Empty(mobile.Certificate);

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
	}

	public class GeneralOptions
	{
		public string Name
		{
			get; set;
		}

		[ConfigurationProperty("Internal")]
		public bool IsInternal
		{
			get; set;
		}

		public CertificateCollection Certificates
		{
			get; set;
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

	public class MobileOptions
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

		public VoicesOptions Voices
		{
			get; set;
		}

		[ConfigurationProperty("Pushing")]
		public NotificationsOptions Notifications
		{
			get; set;
		}
	}

	public class VoicesOptions : Dictionary<string, Template>
	{
		public VoicesOptions() : base(StringComparer.OrdinalIgnoreCase)
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
				{
					var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries);

					for(int i = 0; i < parts.Length; i++)
					{
						parts[i] = parts[i].Trim();
					}

					return parts;
				}

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

	public class NotificationsOptions : Zongsoft.Collections.NamedCollectionBase<App>
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
}
