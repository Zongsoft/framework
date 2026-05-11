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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.Data.Sqlite;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.SQLite;

public partial class SQLiteDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：SQLite。</summary>
	public const string NAME = "SQLite";
	#endregion

	#region 单例字段
	public static readonly SQLiteDriver Instance = new();
	#endregion

	#region 私有构造
	private SQLiteDriver()
	{
		this.Getter = new SQLiteGetter();
		this.Setter = new SQLiteSetter();
		this.Features.Add(Feature.Returning);
	}
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => SQLiteStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override Exception OnError(IDataAccessContext context, Exception exception)
	{
		if(exception is SqliteException error)
		{
			switch(error.SqliteErrorCode)
			{
				case -1:
					break;
			}
		}

		return exception;
	}

	public override DbCommand CreateCommand() => new SqliteCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new SqliteCommand(text)
	{
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) => new SqliteConnector(connectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new SqliteConnectionStringBuilder(connectionString);
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new SQLiteImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new SQLiteExpressionVisitor();
	#endregion

	#region 嵌套子类
	private sealed class SqliteConnector : SqliteConnection
	{
		private static readonly ConcurrentDictionary<string, Pragma[]> _pragmas = new(StringComparer.OrdinalIgnoreCase);

		public SqliteConnector(string connectionString)
		{
			if(string.IsNullOrEmpty(connectionString))
				base.ConnectionString = string.Empty;
			else
			{
				var settings = Configuration.SQLiteConnectionSettingsDriver.Instance.GetSettings(connectionString);
				this.ConnectionString = settings.GetOptions().ConnectionString;

				if(!_pragmas.ContainsKey(this.ConnectionString))
					_pragmas.TryAdd(this.ConnectionString, [.. GetPragmas(settings)]);
			}

			static IEnumerable<Pragma> GetPragmas(Zongsoft.Configuration.IConnectionSettings settings)
			{
				const string PRAGMA_PREFIX = "PRAGMA:";

				foreach(var setting in settings)
				{
					if(setting.Key.StartsWith(PRAGMA_PREFIX, StringComparison.OrdinalIgnoreCase))
						yield return new(setting.Key[PRAGMA_PREFIX.Length..], setting.Value);
				}
			}
		}

		protected override void OnStateChange(StateChangeEventArgs args)
		{
			if(args.CurrentState == ConnectionState.Open)
			{
				if(_pragmas.TryGetValue(this.ConnectionString, out var pragmas) && pragmas != null && pragmas.Length > 0)
				{
					var text = new System.Text.StringBuilder();

					foreach(var pragma in pragmas)
					{
						if(pragma.HasValue)
							text.AppendLine($"PRAGMA {pragma.Name}={pragma.Value};");
						else
							text.AppendLine($"PRAGMA {pragma.Name};");
					}

					var command = this.CreateCommand();
					command.CommandText = text.ToString();
					command.ExecuteNonQuery();

					//重置已经执行过的 PRAGMA 集连接
					_pragmas[this.ConnectionString] = default;
				}
			}

			base.OnStateChange(args);
		}
	}

	private sealed class Pragma(string name, string value = null)
	{
		public readonly string Name = name?.Trim();
		public readonly string Value = value?.Trim();

		public bool HasValue => !string.IsNullOrWhiteSpace(this.Value);
		public override string ToString() => string.IsNullOrEmpty(this.Value) ? this.Name : $"{this.Name}={this.Value}";
	}
	#endregion
}
