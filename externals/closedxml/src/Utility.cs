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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using ClosedXML;
using ClosedXML.Excel;
using ClosedXML.Graphics;

using Zongsoft.Data;

namespace Zongsoft.Externals.ClosedXml;

public static class Utility
{
	/// <summary>将以排版点表示的宽度换算为 ClosedXML 工作表列宽。</summary>
	/// <param name="worksheet">用于获取默认字体度量信息的工作表。</param>
	/// <param name="points">以排版点（1/72 英寸）为单位的宽度。</param>
	/// <returns>换算后的 ClosedXML 列宽，取值范围为 0 到 255。</returns>
	public static double GetColumnWidth(IXLWorksheet worksheet, double points)
	{
		const double POINTS_PER_INCH = 72;
		const double PIXELS_PER_INCH = 96;

		var pixels = Math.Max(1, (int)Math.Round(points * PIXELS_PER_INCH / POINTS_PER_INCH));
		var digitWidth = Math.Max(1, (int)Math.Round(DefaultGraphicEngine.Instance.Value.GetMaxDigitWidth(worksheet.Style.Font, PIXELS_PER_INCH)));
		var width = pixels >= digitWidth + 5 ? (pixels - 5d) / digitWidth : (double)pixels / (digitWidth + 5d);
		return Math.Clamp(width, 0, 255);
	}

	public static object GetCellValue(this IXLCell cell, ModelPropertyDescriptor property)
	{
		if(cell == null || cell.IsEmpty() || cell.Value.IsBlank || cell.Value.IsError)
			return null;

		var value = GetCellValue(cell);

		if(property == null || property.Type == null)
			return value.ToString();

		return Common.Convert.ConvertValue(value, property.Type);
	}

	public static object GetCellValue(this IXLCell cell)
	{
		if(cell.HasFormula)
			return cell.GetFormattedString();

		return cell.Value.Type switch
		{
			XLDataType.Blank => null,
			XLDataType.Text => cell.GetText(),
			XLDataType.Number => cell.GetDouble(),
			XLDataType.Boolean => cell.GetBoolean(),
			XLDataType.DateTime => cell.GetDateTime(),
			XLDataType.TimeSpan => cell.GetTimeSpan(),
			XLDataType.Error => cell.GetError().ToString(),
			_ => null,
		};
	}

	public static void SetCellValue(this IXLCell cell, object value)
	{
		if(value == null || Convert.IsDBNull(value))
		{
			cell.Value = Blank.Value;
			return;
		}

		cell.Value = Type.GetTypeCode(value.GetType()) switch
		{
			TypeCode.String => (string)value,
			TypeCode.Byte => (byte)value,
			TypeCode.SByte => (sbyte)value,
			TypeCode.Int16 => (short)value,
			TypeCode.Int32 => (int)value,
			TypeCode.Int64 => (long)value,
			TypeCode.UInt16 => (ushort)value,
			TypeCode.UInt32 => (uint)value,
			TypeCode.UInt64 => (ulong)value,
			TypeCode.Single => (float)value,
			TypeCode.Double => (double)value,
			TypeCode.Decimal => (decimal)value,
			TypeCode.Boolean => (bool)value,
			TypeCode.DateTime => (DateTime)value,
			_ => value.ToString(),
		};
	}
}
