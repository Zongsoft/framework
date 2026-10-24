/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2015-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using Zongsoft.Data.Features;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql.Features;

internal sealed class MySqlReturningFeature : ReturningFeature
{
	#region 重写方法
	public override bool Supports(DataAccessMethod method, ReturningKind kind) =>
		method is DataAccessMethod.Update or DataAccessMethod.Delete && base.Supports(method, kind);

	protected override async ValueTask<bool> OnExecuteAsync(IDataMutateContext context, IMutateStatement mutation,
		Func<CancellationToken, ValueTask<bool>> execute,
		Func<DbDataReader, CancellationToken, ValueTask<bool>> populate, CancellationToken cancellation)
	{
		if(mutation is not MutateStatement statement || statement is not (UpdateStatement or DeleteStatement))
			throw new NotSupportedException();

		Validate(statement);

		if(context.Source.Features.Get<ITransactionFeature>()?.Suppressed == true)
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

		var older = returning.Members.Where(member => member.Kind == ReturningKind.Older).ToArray();
		for(int i = 0; i < older.Length; i++)
			selection.Select.Members.Add(new FieldIdentifier(older[i].Field.Table, older[i].Field.Name, $"__value{i}"));

		selection.Select.IsDistinct = true;
		var visitor = new ParameterVisitor();
		visitor.Visit(selection);

		foreach(var parameter in visitor.Parameters)
			selection.Parameters.Add(parameter);

		var temporary = TableDefinition.Temporary();
		var name = Quote(temporary.Name);
		var fields = keys.Select((_, index) => Quote($"__key{index}")).ToArray();
		var created = false;

		try
		{
			await VerifyTablesAsync(context, statement, lease.Connection, lease.Transaction, cancellation);
			int count;

			await using(var command = context.Source.Driver.CreateCommand(context, selection))
			{
				command.Connection = lease.Connection;
				command.Transaction = lease.Transaction;
				StatementSlotter.Evaluate(context, selection, command);
				await selection.BindAsync(context, command, cancellation);

				foreach(var parameter in selection.Parameters)
					parameter.Value = command.Parameters[parameter.Name].Value;

				var text = command.CommandText.TrimEnd().TrimEnd(';');
				created = true;
				command.CommandText = $"CREATE TEMPORARY TABLE {name} (PRIMARY KEY ({string.Join(',', fields)})) ENGINE=InnoDB AS {text} LIMIT 0;";
				await command.ExecuteNonQueryAsync(cancellation);
				command.CommandText = $"INSERT INTO {name} {text} FOR UPDATE;";
				count = await command.ExecuteNonQueryAsync(cancellation);
			}

			if(count == 0)
				return false;

			//锁定读取持有元数据锁后再次核实引擎，防止检查与读取之间发生表转换
			await VerifyTablesAsync(context, statement, lease.Connection, lease.Transaction, cancellation);

			var stable = new SelectStatement(temporary.Identifier("K"));
			var condition = ConditionExpression.And();

			for(int i = 0; i < keys.Length; i++)
			{
				var field = stable.Table.CreateField($"__key{i}");
				stable.Select.Members.Add(field);
				condition.Add(Expression.Equal(field, statement.Table.CreateField(keys[i])));
			}

			stable.Where = condition;
			statement.Where = Expression.Exists(stable);
			statement.Returning = null;
			await execute(cancellation);

			var query = new SelectStatement(stable.Table);
			if(statement is UpdateStatement)
			{
				var join = new JoinClause(string.Empty, statement.Table, JoinType.Inner);
				join.Conditions.Add(condition);
				query.From.Add(join);
				query.Select.Members.Add(new AggregateExpression(DataAggregateFunction.Count, false, Expression.Constant(1)));

				await using var check = context.Source.Driver.CreateCommand(context, query);
				check.Connection = lease.Connection;
				check.Transaction = lease.Transaction;

				if(Convert.ToInt64(await check.ExecuteScalarAsync(cancellation)) != count)
					throw new NotSupportedException();

				query.Select.Members.Clear();
				foreach(var member in returning.Members)
				{
					if(member.Kind == ReturningKind.Newer)
						query.Select.Members.Add(member.Field);
				}
			}

			for(int i = 0; i < older.Length; i++)
				query.Select.Members.Add(stable.Table.CreateField($"__value{i}", older[i].Field.Alias ?? older[i].Field.Name));

			statement.Returning = returning;
			await using var result = context.Source.Driver.CreateCommand(context, query);
			result.Connection = lease.Connection;
			result.Transaction = lease.Transaction;
			await using var reader = await result.ExecuteReaderAsync(cancellation);
			await populate(reader, cancellation);
			return true;
		}
		finally
		{
			statement.Where = where;
			statement.Returning = returning;

			if(created)
				await ExecuteCommandAsync(context, lease.Connection, lease.Transaction, $"DROP TEMPORARY TABLE IF EXISTS {name};", CancellationToken.None);
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
		var tables = statement.From
			.Select(source => source is JoinClause join ? join.Target : source)
			.OfType<TableIdentifier>()
			.Select(table => table.Name)
			.Distinct(StringComparer.OrdinalIgnoreCase);

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
	private static async ValueTask ExecuteCommandAsync(IDataMutateContext context, DbConnection connection, DbTransaction transaction, string text, CancellationToken cancellation)
	{
		await using var command = context.Source.Driver.CreateCommand(text);
		command.Connection = connection;
		command.Transaction = transaction;
		await command.ExecuteNonQueryAsync(cancellation);
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
