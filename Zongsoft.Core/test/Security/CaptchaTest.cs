using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Security
{
	public class CaptchaTest
	{
		[Fact]
		public void TestParse()
		{
			Captcha result;

			Assert.False(Captcha.TryParse("", out result));
			Assert.True(result.IsEmpty);
			Assert.False(Captcha.TryParse("  ", out result));
			Assert.True(result.IsEmpty);

			Assert.True(Captcha.TryParse(@"scheme:token=data?extra", out result));
			Assert.Equal("scheme", result.Scheme);
			Assert.Equal("token", result.Token);
			Assert.Equal("data", result.Data);
			Assert.Equal("extra", result.Extra);

			Assert.True(Captcha.TryParse(@"scheme:token?extra", out result));
			Assert.Equal("scheme", result.Scheme);
			Assert.Equal("token", result.Token);
			Assert.Null(result.Data);
			Assert.Equal("extra", result.Extra);

			Assert.True(Captcha.TryParse(@"scheme:token", out result));
			Assert.Equal("scheme", result.Scheme);
			Assert.Equal("token", result.Token);
			Assert.Null(result.Data);
			Assert.Null(result.Extra);

			Assert.True(Captcha.TryParse(@"scheme:token=", out result));
			Assert.Equal("scheme", result.Scheme);
			Assert.Equal("token", result.Token);
			Assert.Null(result.Data);
			Assert.Null(result.Extra);

			Assert.True(Captcha.TryParse(@"scheme:token?", out result));
			Assert.Equal("scheme", result.Scheme);
			Assert.Equal("token", result.Token);
			Assert.Null(result.Data);
			Assert.Null(result.Extra);

			Assert.True(Captcha.TryParse(@"scheme:token=?", out result));
			Assert.Equal("scheme", result.Scheme);
			Assert.Equal("token", result.Token);
			Assert.Null(result.Data);
			Assert.Null(result.Extra);
		}
	}
}
