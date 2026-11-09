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

using Microsoft.Data.SqlClient;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql;

public partial class MsSqlDriver : DataDriverBase
{
	#region 公共常量
	/// <summary>驱动程序的标识：MsSql。</summary>
	public const string NAME = "MsSql";
	#endregion

	#region 单例字段
	public static readonly MsSqlDriver Instance = new();
	#endregion

	#region 私有构造
	private MsSqlDriver()
	{
		this.Features.Set<IReturningFeature>(ReturningFeature.Instance);
		this.Features.Set<IConnectionSharingFeature>(new MultipleActiveResultSetsFeature());
		this.Setter = new MsSqlSetter();
	}
	#endregion

	#region 公共属性
	public override string Name => NAME;
	public override IStatementBuilder Builder => MsSqlStatementBuilder.Default;
	#endregion

	#region 公共方法
	public override ValueTask<Exception> OnErrorAsync(IDataAccessContext context, Exception exception, CancellationToken cancellation = default)
	{
		if(exception is SqlException error)
		{
			switch(error.Number)
			{
				case 2601:
				case 2627:
					if(TryGetConflict(error.Message, out var key, out var value))
						return new ValueTask<Exception>(new DataConflictException(this.Name, error.Number, key, value));
					else
						return new ValueTask<Exception>(new DataConflictException(this.Name, error.Number, null, null, error));
			}
		}

		return new ValueTask<Exception>(exception);
	}

	public override DbCommand CreateCommand() => new SqlCommand();
	public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new SqlCommand(text)
	{
		CommandType = commandType,
	};

	public override DbConnection CreateConnection(string connectionString = null) =>
		new SqlConnection(Configuration.MsSqlConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions().ConnectionString);
	public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
		Configuration.MsSqlConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();
	#endregion

	#region 保护方法
	protected override IDataImporter CreateImporter() => new MsSqlImporter();
	protected override ExpressionVisitorBase CreateVisitor() => new MsSqlExpressionVisitor();
	protected override void SetParameter(DbParameter parameter, ParameterExpression expression)
	{
		base.SetParameter(parameter, expression);

		parameter.DbType = expression.Type.DbType switch
		{
			DbType.SByte => DbType.Byte,
			DbType.UInt16 => DbType.Int16,
			DbType.UInt32 => DbType.Int32,
			DbType.UInt64 => DbType.Int64,
			DbType.Object when expression.Type.Name.Equals("text", StringComparison.OrdinalIgnoreCase) => DbType.String,
			_ => expression.Type,
		};

		if(expression.Schema != null && expression.Schema.Token.Property.IsSimplex)
			((SqlParameter)parameter).IsNullable = ((Metadata.IDataEntitySimplexProperty)expression.Schema.Token.Property).Nullable;
		else
			((SqlParameter)parameter).IsNullable = false;
	}
	#endregion

	#region 私有方法
	private static bool TryGetConflict(string message, out string key, out string value)
	{
		key = null;
		value = null;

		if(string.IsNullOrEmpty(message))
			return false;

		var end = message.LastIndexOf('\'');
		var start = end > 0 ? message.LastIndexOf('\'', end - 1) : -1;

		if(start > 0 && end > 0)
		{
			key = message.Substring(start + 1, end - start - 1);

			end = message.LastIndexOf('\'', start - 1);
			start = message.IndexOf('\'');

			if(end > 0 && start > 0 && start < end)
				value = message.Substring(start + 1, end - start - 1);

			return true;
		}

		return false;
	}
	#endregion

	#region 嵌套子类
	private sealed class MultipleActiveResultSetsFeature : ConnectionSharingFeature
	{
		public override bool CanShare(IDataSource source)
		{
			ArgumentNullException.ThrowIfNull(source);
			return Configuration.MsSqlConnectionSettingsDriver.Instance.GetSettings(source.ConnectionString).GetOptions().MultipleActiveResultSets;
		}
	}
	#endregion
}
