using System;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Zongsoft.Security.Captcha.Tests;

public class AuthencodeImagerTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("A")]
	[InlineData("AB")]
	public void Generate_ShortCode_ReturnsNullBeforeValidatingDimensions(string code)
	{
		Assert.Null(AuthencodeImager.Generate(code));
		Assert.Null(AuthencodeImager.Generate(code, -1, -1));
	}

	[Theory]
	[InlineData(-1, 50, "width")]
	[InlineData(0, 50, "width")]
	[InlineData(59, 50, "width")]
	[InlineData(120, -1, "height")]
	[InlineData(120, 0, "height")]
	[InlineData(120, 19, "height")]
	public void Generate_InvalidDimensions_ReportsParameter(int width, int height, string parameter)
	{
		Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() => AuthencodeImager.Generate("A7zP", width, height)).ParamName);
	}

	[Fact]
	public void Generate_DefaultDimensions_ProducesRenderedImage()
	{
		using var image = AuthencodeImager.Generate("A7zP");
		Assert.Equal(120, image.Width);
		Assert.Equal(50, image.Height);
		AssertRendered(image);
	}

	[Theory]
	[InlineData("A7z", 60, 20)]
	[InlineData("A7zP", 120, 20)]
	[InlineData("A7zP", 120, 21)]
	[InlineData("A7zP", 120, 31)]
	[InlineData("A7zP", 120, 32)]
	[InlineData("A7z", 60, 50)]
	[InlineData("A7zP9", 160, 64)]
	public void Generate_CustomDimensions_ProducesDecodablePng(string code, int width, int height)
	{
		using var image = AuthencodeImager.Generate(code, width, height);
		AssertRendered(image);
		using var stream = new MemoryStream();
		image.SaveAsPng(stream);
		stream.Position = 0;
		using var decoded = Image.Load<Rgba32>(stream);

		Assert.Equal(width, image.Width);
		Assert.Equal(height, image.Height);
		Assert.Equal(width, decoded.Width);
		Assert.Equal(height, decoded.Height);
	}

	[Theory]
	[InlineData("WWW", 60, 120)]
	[InlineData("MMM", 120, 120)]
	public void Generate_LargeGlyphs_PreservesRequestedDimensions(string code, int width, int height)
	{
		//覆盖随机路径和字体大小下，大字形越过右边界的情况。
		for(int i = 0; i < 16; i++)
		{
			using var image = AuthencodeImager.Generate(code, width, height);
			Assert.Equal(width, image.Width);
			Assert.Equal(height, image.Height);
			AssertRendered(image);
		}
	}

	private static void AssertRendered(Image<Rgba32> image)
	{
		var coloredPixels = 0;
		var white = new Rgba32(255, 255, 255, 255);
		image.ProcessPixelRows(accessor =>
		{
			for(int y = 0; y < accessor.Height; y++)
			{
				foreach(var pixel in accessor.GetRowSpan(y))
				{
					if(pixel != white)
						coloredPixels++;
				}
			}
		});
		Assert.True(coloredPixels > 0);
		Assert.Equal(byte.MaxValue, image[image.Width - 1, image.Height - 1].A);
	}
}
