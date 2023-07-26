/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.ClosedXml library.
 *
 * The Zongsoft.Externals.ClosedXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.ClosedXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.ClosedXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using ClosedXML;
using ClosedXML.Excel;

using Zongsoft.Data;

namespace Zongsoft.Externals.ClosedXml
{
	public static class Utility
	{
		public static object GetCellValue(this IXLCell cell, ModelPropertyDescriptor property)
		{
			if(cell == null || cell.IsEmpty() || cell.Value.IsBlank || cell.Value.IsError)
				return null;

			var value = GetCellValue(cell);

			if(property == null || property.Type == null)
				return value.ToString();

			return Common.Convert.ConvertValue(value, property.Type);
		}

		private static object GetCellValue(this IXLCell cell)
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
}