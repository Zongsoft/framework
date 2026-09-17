using System;
using System.Globalization;

using Xunit;

namespace Zongsoft.Resources.Tests;

public class ExceptionLocalizationTest
{
	[Theory]
	[InlineData("en-US", "The notification queue capacity must be positive.")]
	[InlineData("zh-CN", "通知队列容量必须为正数。")]
	[InlineData("fr-FR", "The notification queue capacity must be positive.")]
	public void Capacity_UsesUICultureAndPreservesArgument(string culture, string message)
	{
		var original = CultureInfo.CurrentUICulture;
		var options = new Caching.DistributedCacheSubscriptionOptions();
		var capacity = options.Capacity;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => options.Capacity = 0);

			Assert.StartsWith(message, exception.Message);
			Assert.Equal("value", exception.ParamName);
			Assert.Equal(0, exception.ActualValue);
			Assert.Equal(capacity, options.Capacity);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[Theory]
	[InlineData("en-US", "The specified 'type' parameter must be a value type.")]
	[InlineData("zh-CN", "指定的“type”参数必须为值类型。")]
	public void Range_FormatsLocalizedMessage(string culture, string message)
	{
		var original = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
			var exception = Assert.Throws<ArgumentException>(() => Data.Range.Create(typeof(string), "1"));

			Assert.Equal(message, exception.Message);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[Theory]
	[InlineData("en-US", "Ago()", "The Ago range function is missing required parameters.")]
	[InlineData("en-US", "Month(2026)", "The Month range function is missing required parameters.")]
	[InlineData("en-US", "Year(2026, 9)", "The Year range function has too many parameters.")]
	[InlineData("zh-CN", "Last()", "Last 范围函数缺少必需的参数。")]
	[InlineData("zh-CN", "Day(2026, 9)", "Day 范围函数缺少必需的参数。")]
	[InlineData("zh-CN", "Day(2026, 9, 17, 1)", "Day 范围函数的参数过多。")]
	public void RangeFunctionErrors_UseSharedMessageWithFunctionName(string culture, string expression, string message)
	{
		var original = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
			var exception = Assert.Throws<ArgumentException>(() => Data.Range<DateTime>.Parse(expression));

			Assert.Equal(message, exception.Message);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[Theory]
	[InlineData("en-US", "Invalid password.")]
	[InlineData("zh-CN", "无效的密码。")]
	public void SecurityReason_PreservesDottedMessageLookup(string culture, string message)
	{
		var original = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
			var exception = new Security.Privileges.AuthenticationException(Security.SecurityReasons.InvalidPassword);

			Assert.Equal(Security.SecurityReasons.InvalidPassword, exception.Reason);
			Assert.Equal(message, exception.Message);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[Theory]
	[InlineData("en-US", 3, "The length of nonce is too short(must be greater than or equal to 4).")]
	[InlineData("en-US", 256, "The length of nonce is too long.")]
	[InlineData("zh-CN", 3, "随机数的长度过短，必须至少为 4。")]
	[InlineData("zh-CN", 256, "随机数的长度过长。")]
	public void Password_InvalidNoncePreservesParameterName(string culture, int length, string message)
	{
		var original = CultureInfo.CurrentUICulture;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
			var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Security.Password.Generate("password", new byte[length]));

			Assert.Equal("nonce", exception.ParamName);
			Assert.StartsWith(message, exception.Message);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}

	[Fact]
	public void ImmutableOptions_ResolveCultureAtThrowTime()
	{
		var original = CultureInfo.CurrentUICulture;
		var options = Serialization.TextSerializationOptions.Default;
		var indented = options.Indented;

		try
		{
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
			var english = Assert.Throws<InvalidOperationException>(() => options.Indented = !indented);
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-CN");
			var chinese = Assert.Throws<InvalidOperationException>(() => options.Indented = !indented);

			Assert.Equal("The serialization options is immutable.", english.Message);
			Assert.Equal("序列化选项不可修改。", chinese.Message);
			Assert.Equal(indented, options.Indented);
		}
		finally
		{
			CultureInfo.CurrentUICulture = original;
		}
	}
}
