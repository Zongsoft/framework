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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineInsertStatementVisitor : InsertStatementVisitor
{
	#region 单例字段
	public static readonly TDengineInsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private TDengineInsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException("Missing required fields in the insert statment.");

		(var tags, var fields) = GetSettings(statement);

		var slot = GenerateSlot(tags);
		statement.Slots.Add(slot);
		context.Write($"INSERT INTO `${{{slot.Name}}}` USING ");
		context.Visit(statement.Table);

		GenerateTags(context, tags);
		GenerateFields(context, fields);

		context.WriteLine(";");
	}
	#endregion

	#region 私有方法
	private static (Setting tags, Setting fields) GetSettings(InsertStatement statement)
	{
		var tags = new Setting();
		var fields = new Setting();

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			var field = statement.Fields[i];
			var value = statement.Values[i];

			if(field.IsTagField())
			{
				tags.Fields.Add(field);
				tags.Values.Add(value);
			}
			else
			{
				fields.Fields.Add(field);
				fields.Values.Add(value);
			}
		}

		return (tags, fields);
	}

	private static StatementSlot GenerateSlot(Setting tags)
	{
		return new StatementSlot("Subtable", "Table", tags.Fields.Select(field => field.Token).ToArray());
	}

	private static void GenerateTags(ExpressionVisitorContext context, Setting tags)
	{
		context.Write(" (");

		for(int i = 0; i < tags.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Visit(tags.Fields[i]);
		}

		context.Write(") TAGS (");

		for(int i = 0; i < tags.Values.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Visit(tags.Values[i]);
		}

		context.WriteLine(")");
	}

	private static void GenerateFields(ExpressionVisitorContext context, Setting fields)
	{
		context.Write(" (");

		for(int i = 0; i < fields.Fields.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Visit(fields.Fields[i]);
		}

		context.Write(") VALUES (");

		for(int i = 0; i < fields.Values.Count; i++)
		{
			if(i > 0)
				context.Write(",");

			context.Visit(fields.Values[i]);
		}

		context.Write(")");
	}
	#endregion

	#region 嵌套子类
	private sealed class Setting
	{
		public readonly IList<FieldIdentifier> Fields = [];
		public readonly IList<IExpression> Values = [];
	}
	#endregion
}
