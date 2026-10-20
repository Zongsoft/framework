using System;
using System.ComponentModel;
using System.Text.Json;

using Xunit;

namespace Zongsoft.Components.Tests;

public class ColorTest
{
	[Theory]
	[InlineData("Red", 255, 0, 0, 0xFFFF0000u, "#FF0000")]
	[InlineData("Green", 0, 128, 0, 0xFF008000u, "#008000")]
	[InlineData("Blue", 0, 0, 255, 0xFF0000FFu, "#0000FF")]
	[InlineData("White", 255, 255, 255, 0xFFFFFFFFu, "#FFFFFF")]
	[InlineData("Black", 0, 0, 0, 0xFF000000u, "#000000")]
	[InlineData("Pink", 255, 192, 203, 0xFFFFC0CBu, "#FFC0CB")]
	[InlineData("Beige", 245, 245, 220, 0xFFF5F5DCu, "#F5F5DC")]
	[InlineData("Coral", 255, 127, 80, 0xFFFF7F50u, "#FF7F50")]
	[InlineData("Indigo", 75, 0, 130, 0xFF4B0082u, "#4B0082")]
	[InlineData("Lavender", 230, 230, 250, 0xFFE6E6FAu, "#E6E6FA")]
	[InlineData("Salmon", 250, 128, 114, 0xFFFA8072u, "#FA8072")]
	[InlineData("Turquoise", 64, 224, 208, 0xFF40E0D0u, "#40E0D0")]
	[InlineData("Chocolate", 210, 105, 30, 0xFFD2691Eu, "#D2691E")]
	[InlineData("Crimson", 220, 20, 60, 0xFFDC143Cu, "#DC143C")]
	public void NamedColor_NameOrDifferentCase_NormalizesToCanonicalValue(string name, byte red, byte green, byte blue, uint value, string text)
	{
		var expected = GetNamedColor(name);
		var parsedLower = Color.Parse(name.ToLowerInvariant().AsSpan());
		var parsedUpper = Color.Parse(name.ToUpperInvariant().AsSpan());

		expected.GetRgb(out var actualRed, out var actualGreen, out var actualBlue);

		Assert.Equal((byte)0xFF, expected.Alpha);
		Assert.Equal(red, actualRed);
		Assert.Equal(green, actualGreen);
		Assert.Equal(blue, actualBlue);
		Assert.Equal(value, expected.Value);
		Assert.Equal(expected, parsedLower);
		Assert.Equal(expected, parsedUpper);
		Assert.Equal(expected.GetHashCode(), parsedLower.GetHashCode());
		Assert.Equal(text, parsedLower.ToString());
		Assert.Equal(expected, Color.Parse(text.AsSpan()));
	}

	[Fact]
	public void RgbConstructors_CustomValue_RoundTripsThroughText()
	{
		var components = new Color(0x12, 0x34, 0x56);
		var packed = new Color(components.Value);
		uint value = components;
		var text = components.ToString();
		components.GetRgb(out var red, out var green, out var blue);

		Assert.Equal((byte)0xFF, components.Alpha);
		Assert.Equal((byte)0x12, red);
		Assert.Equal((byte)0x34, green);
		Assert.Equal((byte)0x56, blue);
		Assert.Equal(0xFF123456u, components.Value);
		Assert.Equal(components.Value, value);
		Assert.Equal(components, packed);
		Assert.True(components == packed);
		Assert.Equal("#123456", text);
		Assert.True(Color.TryParse(text.AsSpan(), out var parsed));
		Assert.Equal(components, parsed);
		Assert.Equal(components, Color.Parse(text.AsSpan()));
	}

	[Fact]
	public void ArgbConstructor_TransparentValue_RoundTripsWithAlphaChannel()
	{
		var color = new Color(0x7F, 0x12, 0x34, 0x56);
		var transparentRgb = new Color(0x00123456u);
		var text = color.ToString();
		color.GetRgb(out var red, out var green, out var blue);
		transparentRgb.GetRgb(out var transparentRed, out var transparentGreen, out var transparentBlue);

		Assert.Equal((byte)0x7F, color.Alpha);
		Assert.Equal((byte)0x12, red);
		Assert.Equal((byte)0x34, green);
		Assert.Equal((byte)0x56, blue);
		Assert.Equal(0x7F123456u, color.Value);
		Assert.Equal("#7F123456", text);
		Assert.True(Color.TryParse(text.AsSpan(), out var parsed));
		Assert.Equal(color, parsed);
		Assert.Equal(color, new Color(color.Value));
		Assert.Equal(color, JsonSerializer.Deserialize<Color>(JsonSerializer.Serialize(color)));
		Assert.Equal((byte)0, transparentRgb.Alpha);
		Assert.Equal((byte)0x12, transparentRed);
		Assert.Equal((byte)0x34, transparentGreen);
		Assert.Equal((byte)0x56, transparentBlue);
		Assert.Equal(0x00123456u, transparentRgb.Value);
		Assert.Equal("#00123456", transparentRgb.ToString());
		Assert.Equal(transparentRgb, Color.Parse(transparentRgb.ToString().AsSpan()));

		Assert.Equal((byte)0, Color.Transparent.Alpha);
		Assert.Equal(0u, Color.Transparent.Value);
		Assert.Equal(Color.Transparent, Color.Parse("transparent".AsSpan()));
		Assert.Equal(Color.Transparent, Color.Parse(Color.Transparent.ToString().AsSpan()));
		Assert.Equal(Color.Transparent, Color.Parse("#00000000".AsSpan()));
		Assert.Equal(Color.Transparent, new Color(Color.Transparent.Value));
		Assert.Equal("#00000000", Color.Transparent.ToString());
	}

	[Fact]
	public void ZeroValue_DefaultTransparentAndBlack_UsePureArgbEquality()
	{
		var zero = default(Color);

		Assert.Equal(0u, zero.Value);
		Assert.Equal("#00000000", zero.ToString());
		Assert.Equal(zero, Color.Transparent);
		Assert.Equal(zero.GetHashCode(), Color.Transparent.GetHashCode());
		Assert.NotEqual(zero, Color.Black);
		Assert.Equal(0xFF000000u, Color.Black.Value);
		Assert.Equal("#000000", Color.Black.ToString());
		Assert.False(Color.TryParse(default(ReadOnlySpan<char>), out var parsedNull));
		Assert.Equal(zero, parsedNull);
		Assert.False(Color.TryParse(ReadOnlySpan<char>.Empty, out var parsedEmpty));
		Assert.Equal(zero, parsedEmpty);
	}

	[Fact]
	public void IParsable_GenericStaticInterface_ParsesNamedRgbAndArgbText()
	{
		Assert.Equal(Color.Red, Parse<Color>("red"));
		Assert.Equal(new Color(0x12, 0x34, 0x56), Parse<Color>("#123456"));
		Assert.Equal(new Color(0x7F, 0x12, 0x34, 0x56), Parse<Color>("#7F123456"));
		Assert.True(TryParse<Color>("blue", out var parsed));
		Assert.Equal(Color.Blue, parsed);
		Assert.False(TryParse<Color>("invalid", out _));

		static T Parse<T>(string text) where T : IParsable<T> => T.Parse(text, null);
		static bool TryParse<T>(string text, out T result) where T : IParsable<T> => T.TryParse(text, null, out result);
	}

	[Theory]
	[InlineData(" \tRed\r\n", 0xFFFF0000u)]
	[InlineData("  beige  ", 0xFFF5F5DCu)]
	[InlineData("\r\n#123456\t", 0xFF123456u)]
	[InlineData(" #7F123456 ", 0x7F123456u)]
	public void SpanParsing_PaddedNamedRgbAndArgbInputs_ReturnsExpectedValue(string text, uint value)
	{
		var parsed = Color.Parse(text.AsSpan());

		Assert.Equal(value, parsed.Value);
		Assert.True(Color.TryParse(text.AsSpan(), out var tried));
		Assert.Equal(parsed, tried);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" \t\r\n ")]
	[InlineData("Unknown")]
	[InlineData("#12345")]
	[InlineData("#1234567")]
	[InlineData("#GG0000")]
	[InlineData("#GG000000")]
	public void SpanParsing_EmptyOrInvalidInput_ReturnsFalseOrThrows(string text)
	{
		Assert.False(Color.TryParse(text.AsSpan(), out var result));
		Assert.Equal(default, result);
		Assert.Throws<FormatException>(() => Color.Parse(text.AsSpan()));
	}

	[Fact]
	public void Converters_NamedAndRgbColors_RoundTripAsStrings()
	{
		var converter = TypeDescriptor.GetConverter(typeof(Color));
		var custom = new Color(0x12, 0x34, 0x56);
		var converted = Assert.IsType<Color>(converter.ConvertFromInvariantString("Red"));
		var customText = Assert.IsType<string>(converter.ConvertToInvariantString(custom));

		Assert.Equal(Color.Red, converted);
		Assert.Equal("#123456", customText);
		Assert.Equal(custom, Color.Parse(customText.AsSpan()));
		Assert.Throws<FormatException>(() => converter.ConvertFromInvariantString("invalid"));

		var namedJson = JsonSerializer.Serialize(Color.Blue);
		var customJson = JsonSerializer.Serialize(custom);

		Assert.Equal("\"#0000FF\"", namedJson);
		Assert.Equal("\"#123456\"", customJson);
		Assert.Equal(Color.Blue, JsonSerializer.Deserialize<Color>(namedJson));
		Assert.Equal(Color.Blue, JsonSerializer.Deserialize<Color>("\"Blue\""));
		Assert.Equal(custom, JsonSerializer.Deserialize<Color>(customJson));
		Assert.Equal("\"#00000000\"", JsonSerializer.Serialize(default(Color)));
		Assert.Equal(default, JsonSerializer.Deserialize<Color>("null"));
		Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Color>("42"));
	}

	private static Color GetNamedColor(string name) => name switch
	{
		nameof(Color.Red) => Color.Red,
		nameof(Color.Green) => Color.Green,
		nameof(Color.Blue) => Color.Blue,
		nameof(Color.White) => Color.White,
		nameof(Color.Black) => Color.Black,
		nameof(Color.Pink) => Color.Pink,
		nameof(Color.Beige) => Color.Beige,
		nameof(Color.Coral) => Color.Coral,
		nameof(Color.Indigo) => Color.Indigo,
		nameof(Color.Lavender) => Color.Lavender,
		nameof(Color.Salmon) => Color.Salmon,
		nameof(Color.Turquoise) => Color.Turquoise,
		nameof(Color.Chocolate) => Color.Chocolate,
		nameof(Color.Crimson) => Color.Crimson,
		_ => throw new ArgumentOutOfRangeException(nameof(name)),
	};
}
