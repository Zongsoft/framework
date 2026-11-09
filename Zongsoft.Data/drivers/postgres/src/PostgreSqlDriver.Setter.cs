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
using System.Data.Common;

using Npgsql;
using NpgsqlTypes;

using Zongsoft.Data.Common;

namespace Zongsoft.Data.PostgreSql;

partial class PostgreSqlDriver
{
	private sealed class PostgreSqlSetter : IDataParameterSetter
	{
		public void SetValue(DbParameter parameter, object value, DataType type = null)
		{
			var dbType = type == null ? parameter.DbType : type.DbType;

			switch(dbType)
			{
				case DbType.UInt16:
					parameter.DbType = DbType.Int16;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, typeof(short));
					break;
				case DbType.UInt32:
					parameter.DbType = DbType.Int32;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, typeof(int));
					break;
				case DbType.UInt64:
					parameter.DbType = DbType.Int64;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, typeof(long));
					break;
				case DbType.DateTime:
				case DbType.DateTime2:
					//PostgreSQL 的 timestamp without time zone 列按字面值存取，不做时区转换；
					//Npgsql 6+ 会强制将非 Utc 的 DateTime 转换（且 Npgsql 10 拒绝写入 Utc 到无时区列），
					//故此处显式指定 NpgsqlDbType.Timestamp 并剥离 Kind，以保持写入与读取的字面值一致。
					parameter.DbType = DbType.DateTime;
					if(parameter is NpgsqlParameter npgsqlParameter)
						npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Timestamp;
					parameter.Value = value is DateTime datetime ? DateTime.SpecifyKind(datetime, DateTimeKind.Unspecified) : value;
					break;
				default:
					parameter.DbType = dbType;
					parameter.Value = Zongsoft.Common.Convert.ConvertValue(value, DataUtility.AsType(dbType));
					break;
			}
		}
	}
}
