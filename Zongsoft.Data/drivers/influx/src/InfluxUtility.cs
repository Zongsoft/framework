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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.Influx library.
 *
 * The Zongsoft.Data.Influx is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.Influx is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.Influx library. If not, see <http://www.gnu.org/licenses/>.
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
