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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Influx;

internal static class InfluxUtility
{
	public static string GetTableName(string text) => string.IsNullOrEmpty(text) ? null : $"T{text.ToLowerInvariant()}";
	public static string GetTableName(IEnumerable<object> values) => values == null ? null : 'T' + string.Join('_', values.Select(value => value?.ToString()))?.ToLowerInvariant();

	public static InfluxDB3.Client.Write.WritePrecision GetPrecision(string text)
	{
		if(string.IsNullOrEmpty(text))
			return InfluxDB3.Client.Write.WritePrecision.Ms;

		if(Zongsoft.Common.Convert.TryConvertValue<InfluxDB3.Client.Write.WritePrecision>(text, out var value))
			return value;

		return text.ToLowerInvariant() switch
		{
			"milliseconds" => InfluxDB3.Client.Write.WritePrecision.Ms,
			"microseconds" => InfluxDB3.Client.Write.WritePrecision.Us,
			"nanoseconds" => InfluxDB3.Client.Write.WritePrecision.Ns,
			"seconds" => InfluxDB3.Client.Write.WritePrecision.S,
			_ => InfluxDB3.Client.Write.WritePrecision.Ms,
		};
	}

	public static bool IsTimestamp(this FieldIdentifier field) => field != null && false;
	public static bool IsTimestamp(this IDataEntityProperty property) => property != null && property.IsSimplex && property.Hint != null &&
	(
		string.Equals(property.Hint, "Timestamp", StringComparison.OrdinalIgnoreCase)
	);

	public static bool IsTagField(this FieldIdentifier field) => field != null && IsTagField(field.Token.Property);
	public static bool IsTagField(this IDataEntityProperty property) => property != null && property.IsSimplex && property.Hint != null &&
	(
		string.Equals(property.Hint, "Tag", StringComparison.OrdinalIgnoreCase) ||
		string.Equals(property.Hint, "Tagged", StringComparison.OrdinalIgnoreCase)
	);
}
