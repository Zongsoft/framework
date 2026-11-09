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
