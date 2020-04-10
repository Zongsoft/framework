using System;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Configuration
{
	public class OptionConfigurationTest
	{
		[Fact]
		public void Test()
		{
			var configuration = new ConfigurationBuilder()
				.AddOptionFile("Configuration/OptionConfigurationTest.option")
				.Build();

			Assert.NotEmpty(configuration.Providers);

			Assert.Equal("general.name", configuration.GetSection("general:name").Value);
			Assert.Equal("true", configuration.GetSection("general:internal").Value);
			Assert.Equal("main", configuration.GetSection("general:certificates:default").Value);

			Assert.Equal("main", configuration.GetSection("general:certificates:certificate:main:name").Value);
			Assert.Equal("C001", configuration.GetSection("general:certificates:certificate:main:code").Value);
			Assert.Equal("xxxx", configuration.GetSection("general:certificates:certificate:main:secret").Value);

			Assert.Equal("test", configuration.GetSection("general:certificates:certificate:test:name").Value);
			Assert.Equal("C002", configuration.GetSection("general:certificates:certificate:test:code").Value);
			Assert.Equal("zzzz", configuration.GetSection("general:certificates:certificate:test:secret").Value);

			Assert.Equal("Shenzhen", configuration.GetSection("mobile:region").Value);
			Assert.Null(configuration.GetSection("mobile.certificate").Value);

			Assert.Equal("Alarm", configuration.GetSection("mobile:message:template:alarm:name").Value);
			Assert.Equal("SMS_01", configuration.GetSection("mobile:message:template:alarm:code").Value);
			Assert.Equal("Zongsoft", configuration.GetSection("mobile:message:template:alarm:scheme").Value);

			Assert.Equal("400123456, 400666888", configuration.GetSection("mobile:voice:numbers").Value);
			Assert.Equal("Alarm", configuration.GetSection("mobile:voice:template:alarm:name").Value);
			Assert.Equal("TTS_01", configuration.GetSection("mobile:voice:template:alarm:code").Value);
			Assert.Equal("Password.Forget", configuration.GetSection("mobile:voice:template:password.forget:name").Value);
			Assert.Equal("TTS_02", configuration.GetSection("mobile:voice:template:password.forget:code").Value);

			Assert.Equal("wechat", configuration.GetSection("mobile:pushing:app:wechat:key").Value);
			Assert.Equal("A123", configuration.GetSection("mobile:pushing:app:wechat:code").Value);
			Assert.Equal("****", configuration.GetSection("mobile:pushing:app:wechat:secret").Value);
		}
	}
}
