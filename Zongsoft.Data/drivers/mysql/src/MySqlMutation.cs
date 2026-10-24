/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql;

internal static class MySqlMutation
{
	#region 执行方法
	public static async ValueTask<bool> ExecuteAsync(IDataMutateContext context, MutateStatement statement,
		Func<CancellationToken, ValueTask<bool>> mutate,
		Func<DbDataReader, CancellationToken, ValueTask<bool>> populate, CancellationToken cancellation)
	{
		Validate(statement);
		if(context.Source.Features.Support(Feature.TransactionSuppressed))
			throw new NotSupportedException();

		await using var lease = await context.Session.AcquireLeaseAsync(cancellation: cancellation);
		if(lease.Transaction == null)
			throw new NotSupportedException();

		var returning = statement.Returning;
		var where = statement.Where;
		var keys = statement.Entity.Key;
		var selection = new SelectStatement(statement.Table) { Where = where };

		foreach(var source in statement.From)
		{
			if(source != statement.Table)
				selection.From.Add(source);

			if(statement is UpdateStatement && source is JoinClause join)
				selection.Where = ConditionExpression.And(selection.Where, join.Conditions);
		}

		for(int i = 0; i < keys.Length; i++)
			selection.Select.Members.Add(statement.Table.CreateField(keys[i], $"__key{i}"));

		foreach(var member in returning.Members)
			selection.Select.Members.Add(member.Field);

		var visitor = new ParameterVisitor();
		visitor.Visit(selection);
		foreach(var parameter in visitor.Parameters)
			selection.Parameters.Add(parameter);

		await VerifyTablesAsync(context, statement, lease.Connection, lease.Transaction, cancellation);
		using var snapshot = new DataTable { CaseSensitive = true };

		await using(var command = context.Source.Driver.CreateCommand(context, selection))
		{
			command.Connection = lease.Connection;
			command.Transaction = lease.Transaction;
			StatementSlotter.Evaluate(context, selection, command);
			await selection.BindAsync(context, command, cancellation);

			foreach(var parameter in selection.Parameters)
				parameter.Value = command.Parameters[parameter.Name].Value;

			command.CommandText = command.CommandText.TrimEnd().TrimEnd(';') + " FOR UPDATE;";

			await using var reader = await command.ExecuteReaderAsync(cancellation);
			for(int i = 0; i < reader.FieldCount; i++)
				snapshot.Columns.Add(reader.GetName(i), reader.GetFieldType(i));

			snapshot.PrimaryKey = snapshot.Columns.Cast<DataColumn>().Take(keys.Length).ToArray();
			while(await reader.ReadAsync(cancellation))
			{
				cancellation.ThrowIfCancellationRequested();
				var values = new object[reader.FieldCount];
				reader.GetValues(values);

				if(snapshot.Rows.Find(values.Take(keys.Length).ToArray()) == null)
					snapshot.Rows.Add(values);
			}
		}

		if(snapshot.Rows.Count == 0)
			return false;

		//锁定读取持有元数据锁后再次核实引擎，防止检查与读取之间发生表转换。
		await VerifyTablesAsync(context, statement, lease.Connection, lease.Transaction, cancellation);
		var temporary = TableDefinition.Temporary();
		var fields = keys.Select(key => Quote(key.GetFieldName(out _))).ToArray();
		var name = Quote(temporary.Name);
		var created = false;

		try
		{
			created = true;
			await ExecuteAsync(context, lease.Connection, lease.Transaction,
				$"CREATE TEMPORARY TABLE {name} (PRIMARY KEY ({string.Join(',', fields)})) ENGINE=InnoDB AS SELECT {string.Join(',', fields)} FROM {Quote(statement.Table.Name)} WHERE 1=0;", cancellation);
			await InsertKeysAsync(context, snapshot, keys.Length, name, fields, lease.Connection, lease.Transaction, cancellation);

			var stable = new SelectStatement(temporary.Identifier("K"));
			var condition = ConditionExpression.And();

			foreach(var key in keys)
			{
				var field = stable.Table.CreateField(key.GetFieldName(out _));
				stable.Select.Members.Add(field);
				condition.Add(Expression.Equal(field, statement.Table.CreateField(key)));
			}

			stable.Where = condition;
			statement.Where = Expression.Exists(stable);
			statement.Returning = null;
			await mutate(cancellation);

			var newer = returning.Members.Where(member => member.Kind == ReturningKind.Newer).ToArray();
			if(newer.Length > 0 && statement is UpdateStatement)
			{
				var query = new SelectStatement(statement.Table) { Where = statement.Where };
				for(int i = 0; i < keys.Length; i++)
					query.Select.Members.Add(statement.Table.CreateField(keys[i], $"__key{i}"));

				foreach(var member in newer)
					query.Select.Members.Add(member.Field);

				await using var command = context.Source.Driver.CreateCommand(context, query);
				command.Connection = lease.Connection;
				command.Transaction = lease.Transaction;
				await using var reader = await command.ExecuteReaderAsync(cancellation);
				var count = 0;

				while(await reader.ReadAsync(cancellation))
				{
					cancellation.ThrowIfCancellationRequested();
					count++;
					var identity = new object[keys.Length];

					for(int i = 0; i < identity.Length; i++)
						identity[i] = reader.GetValue(i);

					var row = snapshot.Rows.Find(identity);
					for(int i = keys.Length; i < reader.FieldCount; i++)
						row[reader.GetName(i)] = reader.GetValue(i);
				}

				if(count != snapshot.Rows.Count)
					throw new NotSupportedException();
			}

			statement.Returning = returning;
			using var result = snapshot.CreateDataReader();
			await populate(result, cancellation);
			return true;
		}
		finally
		{
			statement.Where = where;
			statement.Returning = returning;
			if(created)
				await ExecuteAsync(context, lease.Connection, lease.Transaction, $"DROP TEMPORARY TABLE IF EXISTS {name};", CancellationToken.None);
		}
	}
	#endregion

	#region 验证方法
	private static void Validate(IStatementBase statement)
	{
		if(statement is IMutateStatement mutation)
		{
			if(mutation.Table.Entity.Key == null || mutation.Table.Entity.Key.Length == 0)
				throw new NotSupportedException();

			if(statement is UpdateStatement update && update.Fields.Any(field => field.Field.Token.Property.IsPrimaryKey()))
				throw new NotSupportedException();
		}

		if(statement.HasSlaves)
		{
			foreach(var slave in statement.Slaves)
				Validate(slave);
		}
	}

	private static async ValueTask VerifyTablesAsync(IDataMutateContext context, Statement statement, DbConnection connection, DbTransaction transaction, CancellationToken cancellation)
	{
		var tables = statement.From.Select(source => source is JoinClause join ? join.Target : source)
			.OfType<TableIdentifier>().Select(table => table.Name).Distinct(StringComparer.OrdinalIgnoreCase);

		foreach(var table in tables)
		{
			await using var command = context.Source.Driver.CreateCommand("SELECT ENGINE FROM information_schema.TABLES WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME=@name;");
			command.Connection = connection;
			command.Transaction = transaction;
			var parameter = command.CreateParameter();
			parameter.ParameterName = "name";
			parameter.Value = table;
			command.Parameters.Add(parameter);
			var engine = await command.ExecuteScalarAsync(cancellation);

			if(engine is not string name || !string.Equals(name, "InnoDB", StringComparison.OrdinalIgnoreCase))
				throw new NotSupportedException();
		}
	}
	#endregion

	#region 辅助方法
	private static string Quote(string name) => "`" + name.Replace("`", "``") + "`";

	private static async ValueTask ExecuteAsync(IDataMutateContext context, DbConnection connection, DbTransaction transaction, string text, CancellationToken cancellation)
	{
		await using var command = context.Source.Driver.CreateCommand(text);
		command.Connection = connection;
		command.Transaction = transaction;
		await command.ExecuteNonQueryAsync(cancellation);
	}

	private static async ValueTask InsertKeysAsync(IDataMutateContext context, DataTable snapshot, int count, string name, string[] fields, DbConnection connection, DbTransaction transaction, CancellationToken cancellation)
	{
		var size = Math.Max(1, 1000 / count);
		foreach(var batch in snapshot.Rows.Cast<DataRow>().Chunk(size))
		{
			await using var command = context.Source.Driver.CreateCommand();
			command.Connection = connection;
			command.Transaction = transaction;
			var rows = new List<string>(batch.Length);

			foreach(var row in batch)
			{
				var names = new string[count];
				for(int i = 0; i < count; i++)
				{
					var parameter = command.CreateParameter();
					parameter.ParameterName = $"p{command.Parameters.Count}";
					parameter.Value = row[i];
					names[i] = "@" + parameter.ParameterName;
					command.Parameters.Add(parameter);
				}

				rows.Add("(" + string.Join(',', names) + ")");
			}

			command.CommandText = $"INSERT INTO {name} ({string.Join(',', fields)}) VALUES {string.Join(',', rows)};";
			await command.ExecuteNonQueryAsync(cancellation);
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class ParameterVisitor : MySqlExpressionVisitor
	{
		public HashSet<ParameterExpression> Parameters { get; } = [];

		protected override void VisitParameter(ExpressionVisitorContext context, ParameterExpression parameter)
		{
			this.Parameters.Add(parameter);
			base.VisitParameter(context, parameter);
		}
	}
	#endregion
}
