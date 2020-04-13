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
		public void TestBindModels()
		{
			var configuration = Models.ModelConfigurationTest.GetConfiguration();

			Assert.NotNull(configuration);
			Assert.NotEmpty(configuration.Providers);

			TestGeneral(configuration.GetOption<General>("/general"));
			TestMobile(configuration.GetOption<Mobile>("mobile"));
		}

		[Fact]
		public void TestBindOptions()
		{
			var configuration = Options.OptionConfigurationTest.GetConfiguration();

			Assert.NotNull(configuration);
			Assert.NotEmpty(configuration.Providers);

			TestGeneral(configuration.GetOption<General>("/general"));
			TestMobile(configuration.GetOption<Mobile>("mobile"));
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
