using System;
using System.Linq;
using System.Globalization;
using System.ComponentModel;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Configuration.Tests;

public class ConnectionSettingsConversionTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void NullableLoaderLimitsPreserveMissingAndExplicitValues(bool specified)
	{
		var settings = new ConversionDriver().GetSettings(specified ? "InputSize=128;MaxRows=5000000000" : string.Empty);
		int? inputSize = specified ? 128 : null;
		long? maxRows = specified ? 5000000000L : null;

		Assert.Equal(inputSize, settings.InputSize);
		Assert.Equal(maxRows, settings.MaxRows);

		var options = settings.GetOptions();
		Assert.Equal(inputSize, options.InputSize);
		Assert.Equal(maxRows, options.MaxRows);
	}

	[Fact]
	public void EnumNameReachesTypedPropertyAndAliasedOptions()
	{
		var settings = new ConversionDriver().GetSettings("ProtocolType=SiemensS7Net_S1200");

		Assert.Equal(SiemensProtocol.SiemensS7Net_S1200, settings.ProtocolType);
		Assert.Equal(SiemensProtocol.SiemensS7Net_S1200, settings.GetOptions().PType);
	}

	[Theory]
	[InlineData("tab,|", "\t|")]
	[InlineData(",", ",")]
	public void LoaderSeparatorConverterPreservesCharactersThroughOptionsAndValue(string text, string expected)
	{
		var driver = new ConversionDriver();
		var settings = driver.GetSettings($"Separators={text};InputSize=128");
		var characters = expected.ToCharArray();

		Assert.Equal(characters, settings.Separators);
		Assert.Equal(characters, settings.GetOptions().Separators);

		settings.Separators = characters;

		var restored = driver.GetSettings(settings.Value);
		Assert.Equal(characters, restored.Separators);
		Assert.Equal(128, restored.InputSize);

		var options = restored.GetOptions();
		Assert.Equal(characters, options.Separators);
		Assert.Equal(128, options.InputSize);
	}

	private sealed class ConversionDriver : ConnectionSettingsDriver<ConversionSettings>
	{
		public ConversionDriver() : base("Core.ConnectionSettingsConversion") { }
	}

	private sealed class ConversionSettings : ConnectionSettingsBase<ConversionDriver, ConversionOptions>
	{
		public ConversionSettings(ConversionDriver driver, string settings) : base(driver, settings) { }
		public ConversionSettings(ConversionDriver driver, string name, string settings) : base(driver, name, settings) { }

		public int? InputSize
		{
			get => this.GetValue<int?>();
			set => this.SetValue(value);
		}

		public long? MaxRows
		{
			get => this.GetValue<long?>();
			set => this.SetValue(value);
		}

		[DefaultValue(SiemensProtocol.SiemensS7Plus)]
		[Alias(nameof(ConversionOptions.PType))]
		public SiemensProtocol ProtocolType
		{
			get => this.GetValue<SiemensProtocol>();
			set => this.SetValue(value);
		}

		[TypeConverter(typeof(SeparatorConverter))]
		public char[] Separators
		{
			get => this.GetValue<char[]>();
			set => this.SetValue(value);
		}
	}

	private sealed class ConversionOptions
	{
		public ConversionOptions() { }

		public int? InputSize { get; set; }
		public long? MaxRows { get; set; }
		public SiemensProtocol PType { get; set; }
		public char[] Separators { get; set; }
	}

	// 仅投影产品使用的协议名称，不引用第三方枚举及其数值。
	private enum SiemensProtocol
	{
		SiemensS7Plus,
		SiemensS7Net_S1200,
	}

	// 复现 Yuanshan modules/learning/abstraction/src/Data/TextFileLoader.Settings.cs 的转换约定。
	private sealed class SeparatorConverter : TypeConverter
	{
		public SeparatorConverter() { }

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is not string text)
				return base.ConvertFrom(context, culture, value);

			if(string.IsNullOrEmpty(text))
				return null;

			if(text == ",")
				return new[] { ',' };

			return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
				.Select(Parse)
				.ToArray();

			static char Parse(string text) => text switch
			{
				"\\t" or "tab" or "Tab" or "TAB" => '\t',
				"\\s" or "space" or "Space" or "SPACE" => ' ',
				_ => text[0],
			};
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(destinationType == typeof(string) && value is char[] separators)
				return string.Join(',', separators.Select(separator => separator == '\t' ? "\\t" : separator.ToString()));

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}
}
