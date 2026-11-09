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

using InfluxDB3.Client;
using InfluxDB3.Client.Config;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

using Zongsoft.Data.Features;

namespace Zongsoft.Data.Influx;

public class InfluxDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：Influx。</summary>
	public const string NAME = "Influx";
	#endregion

	#region 单例字段
	public static readonly InfluxDriver Instance = new();
	#endregion

	#region 私有构造
	private InfluxDriver() => this.Features.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => InfluxStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation = default)
	{
		return new ValueTask<Exception>(exception);
	}

	public override DbCommand CreateCommand() => new Common.InfluxCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new Common.InfluxCommand()
	{
		CommandText = text,
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) =>
		new Common.InfluxConnection(Configuration.InfluxConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions().ConnectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
		Configuration.InfluxConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new InfluxImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new InfluxExpressionVisitor();
	protected override IStatementSlotEvaluator CreateSlotter() => InfluxStatementSlotEvaluator.Instance;
	#endregion
}
