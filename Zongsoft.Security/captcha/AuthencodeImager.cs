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
		if(string.IsNullOrEmpty(code) || code.Length < 3)
			return null;

		if(width < 60)
			throw new ArgumentOutOfRangeException(nameof(width));
		if(height < 20)
			throw new ArgumentOutOfRangeException(nameof(height));

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

		//设置字符的渲染刷子
		var brush = new LinearGradientBrush(points[0], points[^1], GradientRepetitionMode.None, [new ColorStop(0, Color.Navy), new ColorStop(0.5f, Color.Maroon), new ColorStop(1, Color.Navy)]);

		//定义验证码各字符的格式
		var runs = new RichTextRun[code.Length];

		//设置每个验证码字符的格式
		for(int i = 0; i < code.Length; i++)
		{
			//设置字符的字体大小为标准大小的80%至120%之间
			runs[i] = new RichTextRun() { Start = i, End = i + 1, Font = GetFont(GetFontSize(Random.Shared.Next(80, 120) / 100f)) };

			//设置字符的画笔（空心字）
			if(Random.Shared.Next() % code.Length == i)
				runs[i].Pen = Pens.DashDotDot(brush, 1);
		}

		//确保验证码字符中至少有一个空心字
		if(!runs.Any(run => run.Pen != null))
			runs[Random.Shared.Next() % runs.Length].Pen = Pens.DashDotDot(brush, 1);

		//定义字符的渲染设置
		var options = new RichTextOptions(GetFont(GetFontSize()))
		{
			WrappingLength = path.ComputeLength(),
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Left,
			Path = path,
			TextRuns = runs,
		};

		//按照定义的路径生成验证码字形
		IPathCollection glyphs = TextBuilder.GenerateGlyphs(code, path, options);

		image.Mutate(context => context
			.Fill(Color.White)
			.DrawBackgroundNoises(width, height)
			.Draw(Brushes.BackwardDiagonal(Color.White, Color.Gray), 3, path)
			.DrawText(options, code, brush)                                             //绘制渐进色背景的文字
			.DrawText(options, code, Brushes.Percent10(Color.White, Color.Transparent)) //绘制渐进色背景文字上的白点
			.DrawForegroundNoises(width, height)
		);

		return image;

		int GetFontSize(float ratio = 1) => (int)Math.Floor((width * height) / code.Length * 0.022 * ratio);
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

	private static Font GetFont(int size, FontStyle style = FontStyle.Bold)
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

	private static IImageProcessingContext DrawForegroundNoises(this IImageProcessingContext context, int width, int height)
	{
		var count = (int)Math.Floor(width * height * 0.01);

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
		var text = Common.Randomizer.GenerateString(10).AsSpan();

		for(int i = 0; i < text.Length; i++)
		{
			//计算字体大小
			var fontSize = Random.Shared.Next(18, 28);

			//绘制随机字符
			context.DrawText(
				text[i].ToString(),
				GetFont(fontSize),
				Brushes.Solid(GetColor()),
				new PointF(Random.Shared.Next(width - fontSize - 5), Random.Shared.Next(height - fontSize - 5))
			);
		}

		return context;
	}

	private static IImageProcessingContext DrawLines(this IImageProcessingContext context, int width, int height, int count = 10)
	{
		for(int i = 0; i < count; i++)
		{
			var point1 = new PointF(Random.Shared.Next(width), Random.Shared.Next(height));
			var point2 = new PointF(Random.Shared.Next(width), Random.Shared.Next(height));

			var brush = new LinearGradientBrush(point1, point2,
				GradientRepetitionMode.None,
				new ColorStop(0, Color.Gray),
				new ColorStop(1, GetColor()));

			context.DrawLine(brush, Random.Shared.Next(1, 2), point1, point2);
		}

		return context;
	}

	private static IImageProcessingContext DrawArcs(this IImageProcessingContext context, int width, int height, int count = 5)
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
				new ColorStop(0, Color.LightGray),
				new ColorStop(1, GetColor()));

			context.Draw(brush, Random.Shared.Next(1, 3), builder.Build());
			builder.Clear();

			//构建并绘制随机的小圆圈
			radius = Random.Shared.Next(3, 8);
			builder.AddArc(Random.Shared.Next(width), Random.Shared.Next(height), radius, radius, 0, 0, 360);
			context.Draw(GetColor(), Random.Shared.Next(1, 2), builder.Build());
			builder.Clear();
		}

		return context;
	}
	#endregion
}
