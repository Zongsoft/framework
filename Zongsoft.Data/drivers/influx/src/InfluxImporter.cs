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
