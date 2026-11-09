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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using ClickHouse.Driver;
using ClickHouse.Driver.ADO;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

using Zongsoft.Data.Features;

namespace Zongsoft.Data.ClickHouse;

public class ClickHouseDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：ClickHouse。</summary>
	public const string NAME = "ClickHouse";
	#endregion

	#region 单例字段
	public static readonly ClickHouseDriver Instance = new();
	#endregion

	#region 私有构造
	private ClickHouseDriver() => this.Features.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => ClickHouseStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation = default)
	{
		if(exception is DbException error)
		{
			switch(error.ErrorCode)
			{
				case 0:
					return new ValueTask<Exception>(error);
			}
		}

		return new ValueTask<Exception>(exception);
	}

	public override DbCommand CreateCommand() => new ClickHouseCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new ClickHouseCommand { CommandText = text, CommandType = commandType };

	public override DbConnection CreateConnection(string connectionString = null) =>
		new ClickHouseConnection(Configuration.ClickHouseConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions().ConnectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
		Configuration.ClickHouseConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new ClickHouseImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new ClickHouseExpressionVisitor();
	#endregion

	#region 嵌套子类
	private sealed class ClickHouseCommand : global::ClickHouse.Driver.ADO.ClickHouseCommand
	{
		public override int ExecuteNonQuery() => this.Normalize(base.ExecuteNonQuery());
		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => this.Normalize(await base.ExecuteNonQueryAsync(cancellationToken));

		private int Normalize(int count)
		{
			if(count != 0 || string.IsNullOrWhiteSpace(this.CommandText))
				return count;

			var statement = this.CommandText.TrimStart();
			return statement.StartsWith("INSERT ", StringComparison.OrdinalIgnoreCase) ||
				statement.StartsWith("ALTER TABLE ", StringComparison.OrdinalIgnoreCase) && statement.Contains(" UPDATE", StringComparison.OrdinalIgnoreCase) ? 1 : count;
		}
	}
	#endregion
}
