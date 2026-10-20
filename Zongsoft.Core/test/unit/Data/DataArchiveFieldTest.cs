using Xunit;

using Zongsoft.Components;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Data.Tests;

public class DataArchiveFieldTest
{
	[Fact]
	public void Constructor_DefaultPresentationOptions_AreUnspecified()
	{
		var field = new DataArchiveField("Amount", "Balance", "Account balance");

		Assert.Equal("Amount", field.Name);
		Assert.Equal("Balance", field.Label);
		Assert.Equal("Account balance", field.Description);
		Assert.Equal(0d, field.Width);
		Assert.Null(field.Alignment);
		Assert.Null(field.FontName);
		Assert.Equal(0d, field.FontSize);
		Assert.Null(field.FontStyle);
		Assert.Null(field.ForegroundColor);
		Assert.Null(field.BackgroundColor);
		Assert.Null(field.TextMode);
		Assert.Null(field.Format);
	}

	[Fact]
	public void PresentationOptions_CombinedValues_PreserveTechnologyNeutralContract()
	{
		var field = new DataArchiveField("Amount")
		{
			Width = 90,
			Alignment = DataArchiveFieldAlignment.Right,
			FontName = "Aptos",
			FontSize = 11,
			FontStyle = DataArchiveFontStyle.Bold | DataArchiveFontStyle.Italic,
			ForegroundColor = Color.Red,
			BackgroundColor = new Color(0xF1, 0xF2, 0xF3),
			TextMode = DataArchiveFieldTextMode.Shrink,
			Format = "N2",
		};

		Assert.Equal(90d, field.Width);
		Assert.Equal(DataArchiveFieldAlignment.Right, field.Alignment);
		Assert.Equal("Aptos", field.FontName);
		Assert.Equal(11d, field.FontSize);
		Assert.Equal(DataArchiveFontStyle.Bold | DataArchiveFontStyle.Italic, field.FontStyle);
		Assert.Equal(Color.Red, field.ForegroundColor.GetValueOrDefault());
		Assert.Equal(new Color(0xFFF1F2F3u), field.BackgroundColor.GetValueOrDefault());
		Assert.Equal(DataArchiveFieldTextMode.Shrink, field.TextMode);
		Assert.Equal("N2", field.Format);
	}

	[Fact]
	public void ColorOptions_NullMeansUnspecifiedAndTransparentRemainsExplicit()
	{
		var unspecified = new DataArchiveField("Unspecified");
		var transparent = new DataArchiveField("Transparent")
		{
			ForegroundColor = Color.Transparent,
			BackgroundColor = default(Color),
		};

		Assert.Null(unspecified.ForegroundColor);
		Assert.Null(unspecified.BackgroundColor);
		Assert.True(transparent.ForegroundColor.HasValue);
		Assert.True(transparent.BackgroundColor.HasValue);
		Assert.Equal(Color.Transparent, transparent.ForegroundColor.Value);
		Assert.Equal(default, transparent.BackgroundColor.Value);
	}
}
