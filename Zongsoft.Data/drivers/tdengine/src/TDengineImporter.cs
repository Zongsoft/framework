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
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		var count = 0L;
		var super = context.Entity.GetTableName();
		var tags = members.Where(member => member.Property.IsTagField()).ToArray();
		var fields = members.Where(member => !member.Property.IsTagField()).ToArray();
		var tables = new Dictionary<string, Table>();

		await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
		{
			var target = item;
			if(target == null)
				continue;

			var tagValues = new object[tags.Length];
			for(int i = 0; i < tags.Length; i++)
			{
				var prepared = await tags[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
				target = prepared.Target;
				tagValues[i] = prepared.Value;
			}

			var tableName = TDengineUtility.GetTableName(super, tagValues);

			if(string.IsNullOrEmpty(tableName))
				continue;

			if(!tables.TryGetValue(tableName, out var table))
				tables.Add(tableName, table = new Table(super, tagValues));

			var values = new object[fields.Length];
			for(int i = 0; i < fields.Length; i++)
			{
				var prepared = await fields[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
				target = prepared.Target;
				values[i] = TDengineUtility.Convert(prepared.Value);
			}

			table.Rows.Add(values);
		}

		if(tables.Count == 0)
			return;

		var script = $"INSERT INTO ? USING `{super}`\n" +
			$"({string.Join(',', tags.Select(member => '`' + member.Property.GetFieldName() + '`'))}) TAGS({string.Join(',', Enumerable.Repeat('?', tags.Length))})\n" +
			$"({string.Join(',', fields.Select(member => '`' + member.Property.GetFieldName() + '`'))}) VALUES ({string.Join(',', Enumerable.Repeat('?', fields.Length))})";

		await using var client = await GetClientAsync(context, cancellation).ConfigureAwait(false);
		await using var statement = await client.StmtInitAsync(cancellation).ConfigureAwait(false);
		await statement.PrepareAsync(script, cancellation).ConfigureAwait(false);

		foreach(var table in tables.Values)
			count += await table.ExecuteAsync(statement, cancellation).ConfigureAwait(false);

		context.Count = (int)Math.Min(count, int.MaxValue);
	}
	#endregion

	#region 私有方法
	private static Task<ITDengineClientAsync> GetClientAsync(DataImportContext context, CancellationToken cancellation) => context.Session.Connector.ConnectAsync(
		token => DbDriver.OpenAsync((ConnectionStringBuilder)TDengineDriver.Instance.CreateConnectionBuilder(context.Source.ConnectionString), token), cancellation);
	#endregion

	#region 嵌套子类
	private sealed class Table(string super, object[] tags) : IEquatable<Table>
	{
		#region 常量定义
		private const int CHUNK_SIZE = 1000;
		#endregion

		#region 成员字段
		private string _name;
		private readonly string _super = super;
		private readonly object[] _tags = tags;
		private readonly List<object[]> _rows = new(128);
		#endregion

		#region 公共属性
		public string Name => _name ??= TDengineUtility.GetTableName(_super, _tags);
		public List<object[]> Rows => _rows;
		#endregion

		#region 公共方法
		public async ValueTask<long> ExecuteAsync(IStmtAsync statement, CancellationToken cancellation)
		{
			var count = 0L;

			for(int index = 0; index < _rows.Count; index += CHUNK_SIZE)
			{
				await statement.SetTableNameAsync(this.Name, cancellation).ConfigureAwait(false);
				await statement.SetTagsAsync(_tags, cancellation).ConfigureAwait(false);

				var length = Math.Min(CHUNK_SIZE, _rows.Count - index);

				for(int offset = 0; offset < length; offset++)
					await statement.BindRowAsync(_rows[index + offset], cancellation).ConfigureAwait(false);

				await statement.AddBatchAsync(cancellation).ConfigureAwait(false);
				await statement.ExecAsync(cancellation).ConfigureAwait(false);
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
