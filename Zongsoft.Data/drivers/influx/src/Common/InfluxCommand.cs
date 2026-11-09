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

namespace Zongsoft.Data.Influx.Common;

public class InfluxCommand : DbCommand
{
	public InfluxCommand() : this(null, string.Empty) { }
	public InfluxCommand(string commandText, CommandType commandType = CommandType.Text) : this(null, commandText, commandType) { }
	public InfluxCommand(InfluxConnection connection, string commandText, CommandType commandType = CommandType.Text)
	{
		this.CommandText = commandText;
		this.CommandType = commandType;
		this.DbConnection = connection;
		this.DbParameterCollection = new InfluxParameterCollection();
	}

	#region 重写属性
	public override string CommandText { get; set; }
	public override int CommandTimeout { get; set; }
	public override CommandType CommandType { get; set; }
	public override bool DesignTimeVisible { get; set; }
	public override UpdateRowSource UpdatedRowSource { get; set; }
	protected override DbConnection DbConnection { get; set; }
	protected override DbParameterCollection DbParameterCollection { get; }
	protected override DbTransaction DbTransaction { get => null; set => throw new NotSupportedException(); }
	#endregion
	#region 内部属性
	internal InfluxDBClient Client => ((InfluxConnection)this.Connection).Client;
	#endregion

	#region 重写方法
	public override void Cancel() { }
	public override void Prepare() => throw new NotSupportedException();
	public override Task PrepareAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}
	protected override DbParameter CreateDbParameter() => new InfluxParameter();

	public override object ExecuteScalar() => throw new NotSupportedException();
	public override int ExecuteNonQuery() => throw new NotSupportedException();
	public override Task<object> ExecuteScalarAsync(CancellationToken cancellation) => throw new NotSupportedException();
	public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellation) => throw new NotImplementedException();
	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
	protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		return Task.FromResult<DbDataReader>(new InfluxDataReader(this.Client.QueryPoints(this.CommandText, InfluxDB3.Client.Query.QueryType.SQL), cancellation));
	}
	#endregion
}
