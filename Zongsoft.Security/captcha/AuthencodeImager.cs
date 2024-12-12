/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security.Captcha library.
 *
 * The Zongsoft.Security.Captcha is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security.Captcha is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security.Captcha library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Runtime.InteropServices;

using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Zongsoft.Security;

public static class AuthencodeImager
{
	#region 公共方法
	public static Image<Rgba32> Generate(string code, int width = 120, int height = 50)
	{
		if(string.IsNullOrEmpty(code))
			return null;

		//构建指定大小的图像
		var image = new Image<Rgba32>(width, height);
		var points = new PointF[code.Length];

		//计算每个字符的宽度
		var cellWith = (float)Math.Floor((double)width / code.Length);
		//依次设置每个字符的位置
		for(int i = 0; i < points.Length; i++)
		{
			points[i] = new PointF((cellWith * i) + (cellWith / 2), Random.Shared.Next(10, height - 10));
		}

		//构建验证码的绘制路径
		var path = new PathBuilder().AddLines(points).Build();

		//定义验证码字符的渲染设置
		var options = new RichTextOptions(GetFont((int)Math.Floor((width * height) / code.Length * 0.024)))
		{
			WrappingLength = path.ComputeLength(),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			Path = path,
		};

		//按照定义的路径生成验证码字形
		IPathCollection glyphs = TextBuilder.GenerateGlyphs(code, path, options);

		image.Mutate(context => context
			.Fill(Color.White)
			.DrawBackgroundNoises(width, height)
			.Draw(Brushes.BackwardDiagonal(Color.White, Color.Gray), 3, path)
			.DrawText(options, code, Brushes.Percent10(Color.LightGray, Color.Black))
			.DrawForegroundNoises(width, height)
		);

		return image;
	}
	#endregion

	#region 私有方法
	private static Color GetColor()
	{
		var color = Random.Shared.Next();
		var r = (byte)(color & 0xFF);
		var g = (byte)((color >> 8) & 0xFF);
		var b = (byte)((color >> 16) & 0xFF);
		return Color.FromRgba(r, g, b, 128);
	}

	private static Font GetFont(int size = 18, FontStyle style = FontStyle.Bold)
	{
		string name;

		if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			name = "Courier New";
		else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			name = "Monospace";
		else if(RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
			name = "Helvetica";
		else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			name = "Helvetica";
		else
			name = null;

		if(name != null && SystemFonts.TryGet(name, out var family))
			return family.CreateFont(size, style);
		else
			return SystemFonts.Families.First().CreateFont(size, style);
	}

	private static IImageProcessingContext DrawForegroundNoises(this IImageProcessingContext context, int width, int height, int count = 100)
	{
		for(int i = 0; i < count; i++)
		{
			var point = new PointF(Random.Shared.Next(width), Random.Shared.Next(height));
			context.DrawLine(GetColor(), Random.Shared.Next(1, 3), point, point);
		}

		return context;
	}

	private static IImageProcessingContext DrawBackgroundNoises(this IImageProcessingContext context, int width, int height)
	{
		context.DrawLines(width, height);
		context.DrawArcs(width, height);

		//生成随机字符
		var text = Zongsoft.Common.Randomizer.GenerateString(15).AsSpan();

		//绘制随机字符
		for(int i = 0; i < text.Length; i++)
			context.DrawText(text[i].ToString(), GetFont(Random.Shared.Next(20, 28)), Brushes.Solid(GetColor()), new PointF(Random.Shared.Next(width - 20), Random.Shared.Next(height - 20)));

		return context;
	}

	private static IImageProcessingContext DrawLines(this IImageProcessingContext context, int width, int height, int count = 20)
	{
		for(int i = 0; i < count; i++)
		{
			var point1 = new PointF(Random.Shared.Next(width), Random.Shared.Next(height));
			var point2 = new PointF(Random.Shared.Next(width), Random.Shared.Next(height));

			var brush = new LinearGradientBrush(point1, point2,
				GradientRepetitionMode.None,
				new ColorStop(0, Color.White),
				new ColorStop(0.5f, Color.LightGray),
				new ColorStop(1.0f, Color.Gray));

			context.DrawLine(brush, Random.Shared.Next(1, 3), point1, point2);
		}

		return context;
	}

	private static IImageProcessingContext DrawArcs(this IImageProcessingContext context, int width, int height, int count = 10)
	{
		var builder = new PathBuilder();

		for(int i = 0; i < count; i++)
		{
			var radius = Math.Max(20, (int)(Random.Shared.NextSingle() * Math.Min(width, height) * 0.6));
			var x = Random.Shared.Next(width - radius);
			var y = Random.Shared.Next(height - radius);
			var rectangle = new Rectangle(x, y, radius, radius);
			builder.AddArc(rectangle, Random.Shared.Next(300), 0, Random.Shared.Next(100, 360));

			var brush = new LinearGradientBrush(
				new PointF(x, y),
				new PointF(x + radius, y + radius),
				GradientRepetitionMode.None,
				new ColorStop(0, Color.White),
				new ColorStop(0.5f, Color.LightGray),
				new ColorStop(1.0f, Color.Gray));

			context.Draw(brush, Random.Shared.Next(1, 3), builder.Build());
			builder.Clear();

			//构建并绘制随机的小圆圈
			radius = Random.Shared.Next(3, 8);
			builder.AddArc(Random.Shared.Next(width), Random.Shared.Next(height), radius, radius, 0, 0, 360);
			context.Draw(GetColor(), Random.Shared.Next(1, 3), builder.Build());
			builder.Clear();
		}

		return context;
	}
	#endregion
}
