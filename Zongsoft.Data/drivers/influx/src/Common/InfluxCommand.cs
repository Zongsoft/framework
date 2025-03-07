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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.Influx library.
 *
 * The Zongsoft.Data.Influx is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.Influx is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.Influx library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

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

	public override string CommandText { get; set; }
	public override int CommandTimeout { get; set; }
	public override CommandType CommandType { get; set; }
	public override bool DesignTimeVisible { get; set; }
	public override UpdateRowSource UpdatedRowSource { get; set; }
	protected override DbConnection DbConnection { get; set; }
	protected override DbParameterCollection DbParameterCollection { get; }
	protected override DbTransaction DbTransaction { get => null; set => throw new NotSupportedException(); }
	internal InfluxDBClient Client => ((InfluxConnection)this.Connection).Client;

	public override void Cancel() { }
	public override void Prepare() { }
	protected override DbParameter CreateDbParameter() => new InfluxParameter();

	public override object ExecuteScalar() => throw new NotSupportedException();
	public override int ExecuteNonQuery() => throw new NotImplementedException();
	protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new InfluxDataReader(this.Client.QueryPoints(this.CommandText, InfluxDB3.Client.Query.QueryType.SQL));
}
