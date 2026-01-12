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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using TDengine.Driver;
using TDengine.Driver.Client;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.TDengine;

public class TDengineImporter : DataImporterBase
{
	#region 重写方法
	protected override void OnImport(DataImportContext context, MemberCollection members)
	{
		var count = 0L;
		var tags = members.Where(member => member.Property.IsTagField()).ToArray();
		var fields = members.Where(member => !member.Property.IsTagField()).ToArray();
		var tables = new Dictionary<string, Table>();

		foreach(var item in context.Data)
		{
			var target = item;
			if(target == null)
				continue;

			var tagValues = tags.Select(member => member.GetValue(ref target)).ToArray();
			var tableName = TDengineUtility.GetTableName(tagValues);

			if(!tables.TryGetValue(tableName, out var table))
				tables.Add(tableName, table = new Table(tagValues));

			table.Rows.Add([.. fields.Select(member => member.GetValue(ref target))]);
		}

		if(tables.Count == 0)
			return;

		var script = $"INSERT INTO ? USING `{context.Entity.GetTableName()}`\n" +
			$"({string.Join(',', tags.Select(member => '`' + member.Property.GetFieldName() + '`'))}) TAGS({string.Join(',', Enumerable.Repeat('?', tags.Length))})\n" +
			$"({string.Join(',', fields.Select(member => '`' + member.Property.GetFieldName() + '`'))}) VALUES ({string.Join(',', Enumerable.Repeat('?', fields.Length))})";

		using var client = GetClient(context.Source.ConnectionString);
		using var statement = client.StmtInit();
		statement.Prepare(script);

		foreach(var table in tables.Values)
		{
			count += table.Execute(statement);
		}

		context.Count = (int)Math.Min(count, int.MaxValue);
	}

	protected override ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		this.OnImport(context, members);
		return ValueTask.CompletedTask;
	}
	#endregion

	#region 私有方法
	private static ITDengineClient GetClient(string connectionString) => DbDriver.Open((ConnectionStringBuilder)TDengineDriver.Instance.CreateConnectionBuilder(connectionString));
	#endregion

	#region 嵌套子类
	private sealed class Table(object[] tags) : IEquatable<Table>
	{
		#region 常量定义
		private const int CHUNK_SIZE = 1000;
		#endregion

		#region 成员字段
		private string _name;
		private readonly object[] _tags = tags;
		private readonly List<object[]> _rows = new(128);
		#endregion

		#region 公共属性
		public string Name => _name ??= TDengineUtility.GetTableName(_tags);
		public List<object[]> Rows => _rows;
		#endregion

		#region 公共方法
		public long Execute(IStmt statement)
		{
			var chunk = 1;
			var count = 0L;

			statement.SetTableName(this.Name);
			statement.SetTags(_tags);

			for(int i = 0; i < _rows.Count; i++)
			{
				statement.BindRow(_rows[i]);

				if(i == (chunk * CHUNK_SIZE) - 1)
				{
					chunk++;
					statement.AddBatch();
					statement.Exec();
					count += statement.Affected();
				}
			}

			if(_rows.Count % CHUNK_SIZE > 0)
			{
				statement.AddBatch();
				statement.Exec();
				count += statement.Affected();
			}

			return count;
		}
		#endregion

		#region 重写方法
		public bool Equals(Table other) => string.Equals(this.Name, other.Name);
		public override bool Equals(object obj) => obj is Table other && this.Equals(other);
		public override int GetHashCode() => this.Name.GetHashCode();
		public override string ToString() => this.Name;
		#endregion
	}
	#endregion
}