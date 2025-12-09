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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public class UpdateStatementBuilder : IStatementBuilder<DataUpdateContext>
{
	#region 常量定义
	private const string TEMPORARY_ALIAS = "tmp";
	#endregion

	#region 构造函数
	public UpdateStatementBuilder()
	{
		this.OperandConverter = new UpdateOperandConverter();
	}
	#endregion

	#region 公共属性
	public OperandConverter<DataUpdateContext> OperandConverter { get; }
	#endregion

	#region 构建方法
	public IEnumerable<IStatementBase> Build(DataUpdateContext context)
	{
		return [this.Build(context, context.Data)];
	}

	private UpdateStatement Build(DataUpdateContext context, object data)
	{
		var statement = this.CreateStatement(context);

		//获取要更新的数据模式（模式不为空）
		if(!context.Schema.IsEmpty)
		{
			//依次生成各个数据成员的关联（包括它的子元素集）
			foreach(var member in context.Schema.Members)
			{
				this.BuildSchema(context, statement, statement.Table, data, member);
			}
		}

		//生成条件子句
		statement.Where = Where(context, statement);

		if(statement.Fields.Count == 0)
			throw new DataException($"The update statement is missing a required set clause.");

		if(context.Options.HasReturning(out var returning))
		{
			if(context.Source.Features.Support(Feature.Returning))
			{
				statement.Returning ??= new();

				foreach(var column in returning.Columns)
				{
					var field = statement.Table.CreateField(column.Name, column.Alias);
					statement.Returning.Append(field, column.Kind);
				}
			}
			else
			{
				var newers = returning.Columns.Where(column => column.Kind == ReturningKind.Newer);

				if(newers.Any())
				{
					var slave = new SelectStatement();

					foreach(var from in statement.From)
						slave.From.Add(from);

					slave.Where = statement.Where;

					//注：由于从属语句的WHERE子句只是简单的指向父语句的WHERE子句，
					//因此必须手动将父语句的参数依次添加到从属语句中。
					foreach(var parameter in statement.Parameters)
						slave.Parameters.Add(parameter);

					foreach(var newer in newers)
						slave.Select.Members.Add(statement.Table.CreateField(newer.Name));

					statement.Slaves.Add(slave);
				}
			}
		}

		return statement;
	}
	#endregion

	#region 私有方法
	private void BuildSchema(DataUpdateContext context, UpdateStatement statement, TableIdentifier table, object data, SchemaMember member)
	{
		//如果不允许更新主键，则忽略对主键修改的构建
		if(member.Token.Property.IsPrimaryKey() && !context.Options.HasBehaviors(UpdateBehaviors.PrimaryKey))
			return;

		//确认指定成员是否有必须的写入值
		var provided = context.Validate(member.Token.Property, out var value);

		//如果不是批量更新，并且指定成员不是必须要生成的且也没有必须写入的值，则返回
		if(!context.IsMultiple() && !Utility.IsGenerateRequired(ref data, member.Name) && !provided)
			return;

		if(member.Token.Property.IsSimplex)
		{
			//忽略不可变属性
			if(member.Token.Property.Immutable && !provided)
				return;

			//创建当前单值属性的对应的字段标识
			var field = table.CreateField(member.Token);

			if(typeof(Operand).IsAssignableFrom(member.Token.MemberType))
			{
				var operand = (Operand)member.Token.GetValue(data);
				var expression = this.OperandConverter.Convert(context, statement, operand);
				statement.Fields.Add(new FieldValue(field, expression));
			}
			else
			{
				var parameter = provided ?
					Expression.Parameter(field, member, value) :
					Expression.Parameter(field, member);

				statement.Parameters.Add(parameter);

				//字段设置项的值即为参数
				statement.Fields.Add(new FieldValue(field, parameter));
			}
		}
		else
		{
			//不可变复合属性不支持任何写操作，即在修改操作中不能包含不可变复合属性
			if(member.Token.Property.Immutable)
				throw new DataException($"The '{member.FullPath}' is an immutable complex(navigation) property and does not support the update operation.");

			var complex = (IDataEntityComplexProperty)member.Token.Property;

			if(complex.Multiplicity == DataAssociationMultiplicity.Many)
			{
				//注：在构建一对多的导航属性的UPSERT语句时不能指定容器数据(即data参数值为空)，因为批量操作语句不支持在构建阶段绑定到具体数据
				var upserts = UpsertStatementBuilder.BuildUpserts(context, complex.Foreign, data, member, member.Children);

				//将新建的语句加入到主语句的从属集中
				foreach(var upsert in upserts)
				{
					statement.Slaves.Add(upsert);
				}
			}
			else
			{
				if(context.Source.Features.Support(Feature.Updation.Multitable))
					this.BuildComplex(context, statement, data, member);
				else
					this.BuildComplexStandalone(context, statement, data, member);
			}
		}
	}

	private void BuildComplex(DataUpdateContext context, UpdateStatement statement, object data, SchemaMember member)
	{
		if(!member.HasChildren)
			return;

		var table = Join(context.Aliaser, statement, member);
		var container = member.Token.GetValue(data);

		foreach(var child in member.Children)
		{
			if(container is IModel model && !model.HasChanges(child.Name))
				continue;

			BuildSchema(context, statement, table, container, child);
		}
	}

	private void BuildComplexStandalone(DataUpdateContext context, UpdateStatement statement, object data, SchemaMember member)
	{
		if(!member.HasChildren)
			return;

		var complex = (IDataEntityComplexProperty)member.Token.Property;
		statement.Returning = new ReturningClause(TableDefinition.Temporary());

		var slave = new UpdateStatement(complex.Foreign, member);
		statement.Slaves.Add(slave);

		//创建从属更新语句的条件子查询语句
		var selection = new SelectStatement(statement.Returning.Table.Identifier());

		foreach(var link in complex.Links)
		{
			ISource source = statement.Table;

			foreach(var anchor in link.GetAnchors())
			{
				if(anchor.IsComplex)
				{
					source = statement.Join(context.Aliaser, source, (IDataEntityComplexProperty)anchor);
				}
				else
				{
					if(statement.Returning.Table.Field((IDataEntitySimplexProperty)anchor) != null)
						statement.Returning.Append(source.CreateField(anchor.Name), ReturningKind.Older);

					var field = selection.Table.CreateField(anchor);
					selection.Select.Members.Add(field);

					if(selection.Where == null)
						selection.Where = Expression.Equal(field, slave.Table.CreateField(link.ForeignKey));
					else
						selection.Where = Expression.AndAlso(slave.Where,
										  Expression.Equal(field, slave.Table.CreateField(link.ForeignKey)));
				}
			}
		}

		slave.Where = Expression.Exists(selection);

		foreach(var child in member.Children)
		{
			this.BuildSchema(context, slave, slave.Table, member.Token.GetValue(data), child);
		}
	}

	private static TableIdentifier Join(Aliaser aliaser, UpdateStatement statement, SchemaMember schema)
	{
		if(schema == null || schema.Token.Property.IsSimplex)
			return null;

		//获取关联的源
		ISource source = schema.Parent == null ?
						 statement.Table :
						 statement.From[schema.Path];

		//第一步：处理模式成员所在的继承实体的关联
		if(schema.Ancestors != null)
		{
			foreach(var ancestor in schema.Ancestors)
			{
				source = statement.Join(aliaser, source, ancestor, schema.FullPath);
			}
		}

		//第二步：处理模式成员（导航属性）的关联
		var join = statement.Join(aliaser, source, schema);
		var target = (TableIdentifier)join.Target;
		statement.Tables.Add(target);

		//返回关联的目标表
		return target;
	}

	private static IExpression Where(DataUpdateContext context, UpdateStatement statement)
	{
		if(context.IsMultiple() || context.Criteria == null)
		{
			var criteria = new ConditionExpression(ConditionCombination.And);

			foreach(var key in statement.Entity.Key)
			{
				if(!statement.Entity.GetTokens(context.ModelType).TryGetValue(key.Name, out var token))
					throw new DataException($"No required primary key field values were specified for the updation '{statement.Entity.Name}' entity data.");

				var field = statement.Table.CreateField(key);
				var parameter = Expression.Parameter(field, new SchemaMember(token));

				criteria.Add(Expression.Equal(field, parameter));
				statement.Parameters.Add(parameter);
			}

			criteria.Add(statement.Where(context.Validate(), context.Aliaser));

			return criteria.Count > 0 ? criteria : null;
		}

		return statement.Where(context.Validate(), context.Aliaser);
	}
	#endregion

	#region 虚拟方法
	protected virtual UpdateStatement CreateStatement(DataUpdateContext context) => new(context.Entity);
	#endregion

	#region 嵌套子类
	private class UpdateOperandConverter : OperandConverter<DataUpdateContext>
	{
		protected override IExpression GetField(DataUpdateContext context, IStatementBase statement, string name)
		{
			var property = context.Entity.Find(name);

			if(property.IsSimplex)
				return statement.Table.CreateField(property);

			throw new DataException($"The specified '{name}' property is not a simplex property, so it cannot participate in the simple operations.");
		}

		protected override IExpression GetValue(DataUpdateContext context, IStatementBase statement, object value)
		{
			var parameter = Expression.Parameter(Utility.GetDbType(value), value);
			statement.Parameters.Add(parameter);
			return parameter;
		}

		protected override Type GetOperandType(DataUpdateContext context, string name)
		{
			var property = context.Entity.Find(name);

			if(property.IsSimplex)
				return ((IDataEntitySimplexProperty)property).Type.DbType.AsType();

			throw new DataException($"The specified '{name}' property is not a simplex property, so its data type cannot be confirmed.");
		}

		protected override IDataEntity GetEntity(DataUpdateContext context) => context.Entity;
	}
	#endregion
}
