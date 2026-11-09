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

using TDengine.Driver;
using TDengine.Data.Client;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

using Zongsoft.Data.Features;

namespace Zongsoft.Data.TDengine;

public partial class TDengineDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：TDengine。</summary>
	public const string NAME = "TDengine";
	#endregion

	#region 单例字段
	public static readonly TDengineDriver Instance = new();
	#endregion

	#region 私有构造
	private TDengineDriver()
	{
		this.Setter = new TDengineSetter();
		this.Features.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));
	}
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => TDengineStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation = default)
	{
		if(exception is TDengineError error)
		{
			switch(error.Code)
			{
				case 0:
					break;
			}
		}

		return new ValueTask<Exception>(exception);
	}

	public override DbCommand CreateCommand() => new Command();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new Command()
	{
		CommandText = text,
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) => new Connection(connectionString ?? string.Empty);

	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
		Configuration.TDengineConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new TDengineImporter();
	protected override IStatementBinder CreateBinder() => TDengineStatementBinder.Instance;
	protected override ExpressionVisitorBase CreateVisitor() => new TDengineExpressionVisitor();
	protected override IStatementSlotEvaluator CreateSlotter() => TDengineStatementSlotEvaluator.Instance;
	#endregion
}
