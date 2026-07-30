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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Data.DuckDB library.
 *
 * The Zongsoft.Data.DuckDB is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.DuckDB is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.DuckDB library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Text;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using DuckDB.NET.Data;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.DuckDB;

/// <summary>提供基于 DuckDB 的数据批量导入功能。</summary>
/// <remarks>
/// <para>
/// 对于常规数据类型，本导入器使用 DuckDB.NET 的 <see cref="DuckDBAppender"/> 执行批量写入。
/// 由于标准 Appender 要求写入值与目标表的物理列在数量、顺序和类型上完全一致，而数据引擎允许只导入任意字段子集，
/// 因此先按照导入字段创建一个临时表并通过 Appender 批量写入，再以单条 <c>INSERT ... SELECT</c> 语句写入目标表。
/// 该方式既利用了 Appender 的批量写入能力，也保留了字段映射、目标表默认值以及忽略约束冲突等导入语义。
/// </para>
/// <para>对于 <see cref="DbType.Object"/> 表示的数据库自定义类型，运行时无法安全确定 Appender 所需的具体 DuckDB 类型，
/// 因此回退为参数化的逐行 <c>INSERT</c>，由 DuckDB.NET 的参数绑定机制完成自定义类型转换。</para>
/// <para>两种方式均使用独立连接，不会加入环境事务；内部事务仅用于保证当前导入批次在失败时能够完整回滚。</para>
/// </remarks>
public class DuckDBImporter : DataImporterBase
{
	#region 公共方法
	protected override void OnImport(DataImportContext context, MemberCollection members)
	{
		if(!CanAppend(members))
		{
			Insert(context, members);
			return;
		}

		using var connection = (DuckDBConnection)context.Session.Connector.Connect();
		using var transaction = connection.BeginTransaction();

		try
		{
			var temporary = $"__zongsoft_import_{Guid.NewGuid():N}";
			var fields = GetFields(members);

			using(var command = connection.CreateCommand())
			{
				command.Transaction = transaction;
				command.CommandText = $"CREATE TEMP TABLE \"{temporary}\" AS SELECT {fields} FROM \"{context.Entity.GetTableName()}\" WHERE false;";
				command.ExecuteNonQuery();
			}

			using(var appender = connection.CreateAppender(temporary))
			{
				foreach(var item in context.Data)
				{
					var target = item;
					var row = appender.CreateRow();

					for(int i = 0; i < members.Count; i++)
						Append(row, members[i].GetValue(ref target), members[i]);

					row.EndRow();
				}
			}

			using(var command = connection.CreateCommand())
			{
				command.Transaction = transaction;
				command.CommandText = $"{(context.Options.ConstraintIgnored ? "INSERT OR IGNORE" : "INSERT")} INTO \"{context.Entity.GetTableName()}\" ({fields}) SELECT {fields} FROM \"{temporary}\";";
				context.Count += command.ExecuteNonQuery();
			}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		if(!CanAppend(members))
		{
			await InsertAsync(context, members, cancellation);
			return;
		}

		await using var connection = (DuckDBConnection)await context.Session.Connector.ConnectAsync(cancellation);
		await using var transaction = await connection.BeginTransactionAsync(cancellation);

		try
		{
			var temporary = $"__zongsoft_import_{Guid.NewGuid():N}";
			var fields = GetFields(members);

			await using(var command = connection.CreateCommand())
			{
				command.Transaction = transaction;
				command.CommandText = $"CREATE TEMP TABLE \"{temporary}\" AS SELECT {fields} FROM \"{context.Entity.GetTableName()}\" WHERE false;";
				await command.ExecuteNonQueryAsync(cancellation);
			}

			using(var appender = connection.CreateAppender(temporary))
			{
				foreach(var item in context.Data)
				{
					cancellation.ThrowIfCancellationRequested();

					var target = item;
					var row = appender.CreateRow();

					for(int i = 0; i < members.Count; i++)
						Append(row, members[i].GetValue(ref target), members[i]);

					row.EndRow();
				}
			}

			using(var command = connection.CreateCommand())
			{
				command.Transaction = transaction;
				command.CommandText = $"{(context.Options.ConstraintIgnored ? "INSERT OR IGNORE" : "INSERT")} INTO \"{context.Entity.GetTableName()}\" ({fields}) SELECT {fields} FROM \"{temporary}\";";
				context.Count += await command.ExecuteNonQueryAsync(cancellation);
			}

			await transaction.CommitAsync(cancellation);
		}
		catch
		{
			await transaction.RollbackAsync(CancellationToken.None);
			throw;
		}
	}
	#endregion

	#region 私有方法
	private static bool CanAppend(MemberCollection members)
	{
		foreach(var member in members)
		{
			if(!member.IsSimplex(out var property) || property.Type.DbType == DbType.Object)
				return false;
		}

		return true;
	}

	private static string GetFields(MemberCollection members)
	{
		var fields = new StringBuilder();

		foreach(var member in members)
		{
			if(fields.Length > 0)
				fields.Append(',');

			fields.Append($"\"{member.Property.GetFieldName()}\"");
		}

		return fields.ToString();
	}

	private static void Append(IDuckDBAppenderRow row, object value, Member member)
	{
		if(value == null || Convert.IsDBNull(value))
		{
			row.AppendNullValue();
			return;
		}

		member.IsSimplex(out var property);

		switch(property.Type.DbType)
		{
			case DbType.AnsiString:
			case DbType.AnsiStringFixedLength:
			case DbType.String:
			case DbType.StringFixedLength:
			case DbType.Xml:
				row.AppendValue(value.ToString());
				break;
			case DbType.Binary:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<byte[]>(value));
				break;
			case DbType.Boolean:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<bool>(value));
				break;
			case DbType.Byte:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<byte>(value));
				break;
			case DbType.SByte:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<sbyte>(value));
				break;
			case DbType.Date:
				((DuckDBAppenderRow)row).AppendValue((DateOnly?)GetDate(value));
				break;
			case DbType.DateTime:
			case DbType.DateTime2:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<DateTime>(value));
				break;
			case DbType.DateTimeOffset:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<DateTimeOffset>(value));
				break;
			case DbType.Guid:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<Guid>(value));
				break;
			case DbType.Int16:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<short>(value));
				break;
			case DbType.Int32:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<int>(value));
				break;
			case DbType.Int64:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<long>(value));
				break;
			case DbType.Time:
				((DuckDBAppenderRow)row).AppendValue((TimeOnly?)GetTime(value));
				break;
			case DbType.UInt16:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<ushort>(value));
				break;
			case DbType.UInt32:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<uint>(value));
				break;
			case DbType.UInt64:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<ulong>(value));
				break;
			case DbType.Currency:
			case DbType.Decimal:
			case DbType.VarNumeric:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<decimal>(value));
				break;
			case DbType.Double:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<double>(value));
				break;
			case DbType.Single:
				row.AppendValue(Zongsoft.Common.Convert.ConvertValue<float>(value));
				break;
			default:
				throw new DataException(string.Format(Properties.Resources.ResourceManager.GetString("ExpressionVisitor.DataTypeUnsupported.Message"), property.Type));
		}

		static DateOnly GetDate(object value) => value switch
		{
			DateOnly date => date,
			DateTime date => DateOnly.FromDateTime(date),
			DateTimeOffset date => DateOnly.FromDateTime(date.DateTime),
			_ => Zongsoft.Common.Convert.ConvertValue<DateOnly>(value),
		};

		static TimeOnly GetTime(object value) => value switch
		{
			TimeOnly time => time,
			TimeSpan time => TimeOnly.FromTimeSpan(time),
			DateTime time => TimeOnly.FromDateTime(time),
			_ => Zongsoft.Common.Convert.ConvertValue<TimeOnly>(value),
		};
	}

	private static void Insert(DataImportContext context, MemberCollection members)
	{
		using var connection = context.Session.Connector.Connect();
		using var command = GetInsertCommand(context, members, connection);

		using var transaction = connection.BeginTransaction();
		command.Transaction = transaction;

		try
		{
			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
					command.Parameters[i].Value = members[i].GetValue(ref target) ?? DBNull.Value;

				context.Count += command.ExecuteNonQuery();
			}

			transaction.Commit();
		}
		catch
		{
			transaction.Rollback();
			throw;
		}
	}

	private static async ValueTask InsertAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation)
	{
		await using var connection = await context.Session.Connector.ConnectAsync(cancellation);
		await using var command = GetInsertCommand(context, members, connection);

		await using var transaction = await connection.BeginTransactionAsync(cancellation);
		command.Transaction = transaction;

		try
		{
			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < members.Count; i++)
					command.Parameters[i].Value = members[i].GetValue(ref target) ?? DBNull.Value;

				context.Count += await command.ExecuteNonQueryAsync(cancellation);
			}

			await transaction.CommitAsync(cancellation);
		}
		catch
		{
			await transaction.RollbackAsync(CancellationToken.None);
			throw;
		}
	}

	private static DbCommand GetInsertCommand(DataImportContext context, MemberCollection members, DbConnection connection)
	{
		var command = connection.CreateCommand();

		var fields = new StringBuilder();
		var values = new StringBuilder();

		foreach(var member in members)
		{
			if(!member.IsSimplex(out var property))
				continue;

			if(fields.Length > 0)
				fields.Append(',');

			fields.Append($"\"{member.Property.GetFieldName()}\"");

			if(values.Length > 0)
				values.Append(',');

			values.Append('?');

			var parameter = command.CreateParameter();
			parameter.DbType = property.Type;
			command.Parameters.Add(parameter);
		}

		command.CommandText = $"{(context.Options.ConstraintIgnored ? "INSERT OR IGNORE" : "INSERT")} INTO \"{context.Entity.GetTableName()}\" ({fields}) VALUES ({values});";

		return command;
	}
	#endregion
}
