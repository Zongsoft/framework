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
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Security.Cryptography;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

internal static class TDengineUtility
{
	public static string GetTableName(string name, ICollection<object> values)
	{
		if(values == null || values.Count == 0)
			return null;

		var text = new StringBuilder(name);

		foreach(var value in values)
		{
			var content = GetText(value);
			text.Append('|');
			text.Append(content?.Length ?? -1);
			text.Append(':');
			text.Append(content);
		}

		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
		return $"T{System.Convert.ToHexString(hash.AsSpan(0, 12))}";

		static string GetText(object value) => value switch
		{
			null => null,
			DateTime datetime => datetime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
			DateTimeOffset datetime => datetime.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
			byte[] bytes => System.Convert.ToHexString(bytes),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString(),
		};
	}

	public static bool IsTagField(this FieldIdentifier field) => field != null && IsTagField(field.Token.Property);
	public static bool IsTagField(this IDataEntityProperty property) =>
	property != null && property.IsSimplex && property.Hint != null &&
	(
		string.Equals(property.Hint, "Tag", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(property.Hint, "Tagged", StringComparison.OrdinalIgnoreCase)
	);

	public static object Convert(object value)
	{
		if(value is DateTime datetime && datetime.Kind == DateTimeKind.Unspecified)
			return new DateTime(datetime.Ticks, DateTimeKind.Local);

		return value;
	}
}
