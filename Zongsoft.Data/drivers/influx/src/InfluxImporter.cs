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
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using InfluxDB3.Client;
using InfluxDB3.Client.Write;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Influx;

public class InfluxImporter : DataImporterBase
{
	#region 重写方法
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		await using var connection = (Common.InfluxConnection)await context.Session.Connector.ConnectAsync(cancellation);
		var points = await GetPointsAsync(context, members, connection.Configuration.WriteOptions.Precision, cancellation).ConfigureAwait(false);
		await connection.Client.WritePointsAsync(points, null, null, null, cancellation).ConfigureAwait(false);
	}
	#endregion

	#region 私有方法
	private static async ValueTask<List<PointData>> GetPointsAsync(DataImportContext context, MemberCollection members, WritePrecision? precision, CancellationToken cancellation)
	{
		var points = new List<PointData>();

		await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
		{
			var target = item;
			var record = PointDataValues.Measurement(context.Entity.GetTableName());

			foreach(var member in members)
			{
				var prepared = await member.GetValueAsync(target, cancellation).ConfigureAwait(false);
				target = prepared.Target;

				if(member.Property.IsTimestamp())
				{
					var timestamp = GetTimestamp(prepared.Value, precision);

					if(timestamp > 0)
						record.SetTimestamp(timestamp, precision);
				}
				else
				{
					if(member.Property.IsTagField())
						record.SetTag(member.Name, prepared.Value.ToString());
					else
						record.SetField(member.Name, prepared.Value);
				}
			}

			context.Count++;
			points.Add(record.AsPoint());
		}

		return points;
	}

	private static long GetTimestamp(object data, WritePrecision? precision)
	{
		var unit = precision.HasValue && precision.Value == WritePrecision.S ? Zongsoft.Common.TimestampUnit.Second : Zongsoft.Common.TimestampUnit.Millisecond;

		var value = data switch
		{
			DateTimeOffset timestamp => unit == Zongsoft.Common.TimestampUnit.Second ? timestamp.ToUnixTimeSeconds() : timestamp.ToUnixTimeMilliseconds(),
			DateTime datetime => Zongsoft.Common.Timestamp.Unix.ToTimestamp(datetime, unit),
			DateOnly dateonly => Zongsoft.Common.Timestamp.Unix.ToTimestamp(dateonly.ToDateTime(TimeOnly.MinValue), unit),
			_ => 0,
		};

		if(value > 0 && precision.HasValue)
			value = precision.Value switch
			{
				WritePrecision.Us => value * 1000,
				WritePrecision.Ns => value * 1000 * 1000,
				_ => value,
			};

		return value;
	}
	#endregion
}
