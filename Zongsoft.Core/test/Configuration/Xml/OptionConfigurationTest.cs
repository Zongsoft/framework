using System;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Configuration.Xml
{
	public class XmlConfigurationTest
	{
		public static IConfigurationRoot GetConfiguration()
		{
			return new ConfigurationBuilder()
				.AddOptionFile("Configuration/Xml/OptionConfigurationTest.option")
				.Build();
		}

		[Fact]
		public void TestLoad()
		{
			var configuration = GetConfiguration();

			Assert.NotEmpty(configuration.Providers);

			Assert.Equal("general.name", configuration.GetSection("general:name").Value);
			Assert.Equal("true", configuration.GetSection("general:intranet").Value);
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
	}
}
