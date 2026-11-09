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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Influx;

public class InfluxInsertStatementVisitor : InsertStatementVisitor
{
	#region 单例字段
	public static readonly InfluxInsertStatementVisitor Instance = new();
	#endregion

	#region 私有构造
	private InfluxInsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void OnVisit(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(statement.Fields == null || statement.Fields.Count == 0)
			throw new DataException(Properties.Resources.InsertStatement_MissingFields_Message);

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
