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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public class StatementBinder : IStatementBinder
{
	#region 单例字段
	public static readonly StatementBinder Default = new();
	#endregion

	#region 公共方法
	public void Bind(IDataMutateContextBase context, IStatementBase statement, DbCommand command)
	{
		if(statement.Parameters.Count > 0)
		{
			if(Utility.IsMultiple(context.Data, out var items))
			{
				var lines = statement switch
				{
					InsertStatement insertion => insertion.Values.Chunk(insertion.Fields.Count).ToArray(),
					UpsertStatement upsertion => upsertion.Values.Chunk(upsertion.Fields.Count).ToArray(),
					_ => [],
				};

				if(lines != null && lines.Length > 0)
				{
					var index = 0;
					var sequences = GetSequenceFields(statement);

					foreach(var item in items)
					{
						if(sequences != null)
							SetSequenceValue(context, sequences, item);

						Bind(context, command, item, lines[index++].OfType<ParameterExpression>());
					}
				}
			}
			else
			{
				var sequences = GetSequenceFields(statement);
				if(sequences != null)
					SetSequenceValue(context, sequences, context.Data);

				Bind(context, command, context.Data, statement.Parameters);
			}
		}

		this.OnBound(context, statement, command);
	}

	public async ValueTask BindAsync(IDataMutateContextBase context, IStatementBase statement, DbCommand command, CancellationToken cancellation = default)
	{
		if(statement.Parameters.Count > 0)
		{
			if(Utility.IsMultiple(context.Data, out var items))
			{
				var lines = statement switch
				{
					InsertStatement insertion => insertion.Values.Chunk(insertion.Fields.Count).ToArray(),
					UpsertStatement upsertion => upsertion.Values.Chunk(upsertion.Fields.Count).ToArray(),
					_ => null,
				};

				if(lines != null && lines.Length > 0)
				{
					var index = 0;
					var sequences = GetSequenceFields(statement);

					foreach(var item in items)
					{
						if(sequences != null)
							await SetSequenceValueAsync(context, sequences, item, cancellation);

						Bind(context, command, item, lines[index++].OfType<ParameterExpression>());
					}
				}
			}
			else
			{
				var sequences = GetSequenceFields(statement);
				if(sequences != null)
					await SetSequenceValueAsync(context, sequences, context.Data, cancellation);

				Bind(context, command, context.Data, statement.Parameters);
			}
		}

		this.OnBound(context, statement, command);
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnBound(IDataMutateContextBase context, IStatementBase statement, DbCommand command) { }
	#endregion

	#region 私有方法
	private static void Bind(IDataMutateContextBase context, DbCommand command, object data, IEnumerable<ParameterExpression> parameters)
	{
		if(data == null || parameters == null)
			return;

		foreach(var parameter in parameters)
		{
			var dbParameter = command.Parameters[parameter.Name];

			if(dbParameter.Direction == ParameterDirection.Input || dbParameter.Direction == ParameterDirection.InputOutput)
			{
				if(parameter.Schema == null || parameter.IsChanged)
				{
					if(parameter.Value is IDataValueBinder binder)
						SetParameterValue(context, dbParameter, binder.Bind(context, data, TryGetParameterValue(data, parameter.Schema, null, out var value) ? value : null));
					else
						SetParameterValue(context, dbParameter, parameter.Value);

					/*
					 * 对于Schema不为空（即表示该参数对应有数据成员），同时还设置了参数值的情况，
					 * 说明该参数值是数据提供程序或导航连接所得，因此必须将其值写回对应的数据项中。
					 */
					if(parameter.Schema != null)
					{
						if(parameter.Schema.Parent == null || !parameter.Schema.Parent.Token.IsMultiple)
							parameter.Schema.Token.SetValue(ref data, parameter.IsChanged && parameter.Value is not IDataValueBinder ? parameter.Value : dbParameter.Value);
						else
						{
							//parameter.Schema.Token.SetValue(parameter.Schema.Parent.Token.GetValue(data), parameter.IsChanged && parameter.Value is not IDataValueBinder ? parameter.Value : dbParameter.Value);
							parameter.Schema.Token.SetValue(ref data, parameter.IsChanged && parameter.Value is not IDataValueBinder ? parameter.Value : dbParameter.Value);
						}
					}
				}
				else if(data != null)
				{
					SetParameterValue(
						context,
						dbParameter,
						GetParameterValue(data, parameter.Schema, dbParameter.DbType));
				}
			}

			if(dbParameter.Value == null)
				dbParameter.Value = DBNull.Value;
		}
	}

	private static void SetParameterValue(IDataMutateContextBase context, DbParameter parameter, object value)
	{
		var setter = context.GetFeature<IDataParameterSetter>();

		if(setter == null)
			parameter.Value = value ?? DBNull.Value;
		else
			setter.SetValue(parameter, value);
	}

	private static bool TryGetParameterValue(object data, SchemaMember member, DbType? dbType, out object value)
	{
		value = null;

		//尝试递归解析当前成员对应的所属数据
		data = Recursive(data, member);

		if(data is IModel model)
		{
			if(model.HasChanges(member.Name))
			{
				value = member.Token.GetValue(data, dbType.HasValue ? dbType.Value.AsType() : null);
				return true;
			}

			return false;
		}

		if(data is IDataDictionary dictionary)
		{
			if(dictionary.HasChanges(member.Name))
			{
				value = member.Token.GetValue(data, dbType.HasValue ? dbType.Value.AsType() : null);
				return true;
			}

			return false;
		}

		value = member.Token.GetValue(data, dbType.HasValue ? dbType.Value.AsType() : null);
		return true;

		static object Recursive(object data, SchemaMember member)
		{
			if(data == null || member == null || member.Parent == null || member.Parent.Token.IsMultiple)
				return data;

			var stack = new Stack<SchemaMember>();

			while(member.Parent != null)
			{
				stack.Push(member.Parent);
				member = member.Parent;
			}

			while(stack.Count > 0)
			{
				member = stack.Pop();

				if(member.Token.TryGetValue(data, null, out var value))
				{
					if(value == null)
						return null;

					data = value;
				}
				else
					return data;
			}

			return data;
		}
	}

	private static object GetParameterValue(object data, SchemaMember member, DbType? dbType)
	{
		return TryGetParameterValue(data, member, dbType, out var value) ? value : ((IDataEntitySimplexProperty)member.Token.Property).DefaultValue;
	}

	private static List<FieldIdentifier> GetSequenceFields(IStatementBase statement)
	{
		List<FieldIdentifier> result = null;
		IEnumerable<FieldIdentifier> fields = statement switch
		{
			InsertStatement insertion => insertion.Fields,
			UpsertStatement upsertion => upsertion.Fields,
			_ => [],
		};

		foreach(var field in fields)
		{
			if(field.Token.Property.IsSimplex(out var simplex))
			{
				var sequence = simplex.Sequence;

				if(sequence != null && sequence.IsExternal)
				{
					result ??= [];
					result.Add(field);
				}
			}
		}

		return result;
	}

	private static bool CanSequence(IDataMutateOptions options, object value)
	{
		var behavior = options switch
		{
			IDataInsertOptions insertion => insertion.SequenceBehavior,
			IDataUpsertOptions upsertion => upsertion.SequenceBehavior,
			_ => DataSequenceBehavior.Never,
		};

		return behavior switch
		{
			DataSequenceBehavior.Alway => true,
			DataSequenceBehavior.Never => false,
			_ => value == null || Convert.IsDBNull(value) || Zongsoft.Common.Convert.IsZero(value),
		};
	}

	private static void SetSequenceValue(IDataMutateContextBase context, IEnumerable<FieldIdentifier> sequenceFileds, object data)
	{
		if(data == null || sequenceFileds == null)
			return;

		foreach(var field in sequenceFileds)
		{
			if(field.Token.Property.IsSimplex(out var simplex))
			{
				var sequence = simplex.Sequence;

				if(sequence != null && sequence.IsExternal)
				{
					var value = field.Token.GetValue(data);

					if(CanSequence(context.Options, value))
					{
						var id = ((DataAccess)context.DataAccess).Increase(context, sequence, data);
						field.Token.SetValue(ref data, Convert.ChangeType(id, field.Token.MemberType));
					}
				}
			}
		}
	}

	private static async ValueTask SetSequenceValueAsync(IDataMutateContextBase context, IEnumerable<FieldIdentifier> sequenceFileds, object data, CancellationToken cancellation)
	{
		if(data == null || sequenceFileds == null)
			return;

		foreach(var field in sequenceFileds)
		{
			if(field.Token.Property.IsSimplex(out var simplex))
			{
				var sequence = simplex.Sequence;

				if(sequence != null && sequence.IsExternal)
				{
					var value = field.Token.GetValue(data);

					if(CanSequence(context.Options, value))
					{
						var id = await ((DataAccess)context.DataAccess).IncreaseAsync(context, sequence, data, cancellation);
						field.Token.SetValue(ref data, Convert.ChangeType(id, field.Token.MemberType));
					}
				}
			}
		}
	}
	#endregion
}
