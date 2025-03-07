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

namespace Zongsoft.Data.Influx
{
	public class InfluxImporter : DataImporterBase
	{
		#region 重写方法
		protected override void OnImport(DataImportContext context, MemberCollection members)
		{
			var connection = (Common.InfluxConnection)context.Session.Source.Driver.CreateConnection(context.Session.Source.ConnectionString);
			connection.Open();
			WritePoints(connection, GetPoints(context, members, connection.Configuration.WriteOptions.Precision));

			static async void WritePoints(Common.InfluxConnection connection, IEnumerable<PointData> points)
			{
				try
				{
					await connection.Client.WritePointsAsync(points);
					connection.Dispose();
				}
				catch { throw; }
			}
		}

		protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
		{
			using var connection = (Common.InfluxConnection)context.Session.Source.Driver.CreateConnection(context.Session.Source.ConnectionString);
			await connection.OpenAsync(cancellation);
			await connection.Client.WritePointsAsync(GetPoints(context, members, connection.Configuration.WriteOptions.Precision), null, null, null, cancellation);
		}
		#endregion

		#region 私有方法
		private static IEnumerable<PointData> GetPoints(DataImportContext context, MemberCollection members, WritePrecision? precision)
		{
			if(context == null || context.Data == null)
				yield break;

			foreach(var item in context.Data)
			{
				var record = PointDataValues.Measurement(context.Entity.GetTableName());

				foreach(var member in members)
				{
					if(member.Property.IsTimestamp())
					{
						var timestamp = GetTimestamp(item, member, precision);

						if(timestamp > 0)
							record.SetTimestamp(timestamp, precision);
					}
					else
					{
						var data = item;

						if(member.Property.IsTagField())
							record.SetTag(member.Name, member.GetValue(ref data).ToString());
						else
							record.SetField(member.Name, member.GetValue(ref data));
					}
				}

				context.Count++;
				yield return record.AsPoint();
			}
		}

		private static long GetTimestamp(object data, Member member, WritePrecision? precision)
		{
			var unit = precision.HasValue && precision.Value == WritePrecision.S ? Zongsoft.Common.TimestampUnit.Second : Zongsoft.Common.TimestampUnit.Millisecond;

			var value = member.GetValue(ref data) switch
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
}