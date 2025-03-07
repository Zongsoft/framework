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

public class InfluxConnection : DbConnection
{
	private ConnectionState _state;
	private InfluxDBClient _client;
	private ClientConfig _configuration;
	private InfluxConnectionStringBuilder _builder;

	public InfluxConnection(string connectionString = null)
	{
		this.ConnectionString = connectionString;
		_builder = new(connectionString);
	}

	public override string ConnectionString { get; set; }
	public override string Database => _builder.Database;
	public override string DataSource => _builder.Server;
	public override string ServerVersion => string.Empty;
	public override ConnectionState State => _state;
	internal InfluxDBClient Client => _client;
	internal ClientConfig Configuration => _configuration;

	public override void Open()
	{
		if(_client != null)
			return;

		_state = ConnectionState.Connecting;
		_configuration = _builder.GetConfiguration();
		_client ??= new InfluxDBClient(_configuration);
		_state = ConnectionState.Open;
	}

	public override void Close()
	{
		//_client?.Dispose();
		//_client = null;
		_state = ConnectionState.Closed;
	}

	protected override void Dispose(bool disposing)
	{
		//this.Close();
		base.Dispose(disposing);
	}

	protected override DbCommand CreateDbCommand() => new InfluxCommand();
	public override void ChangeDatabase(string database) => _builder.Database = database;
	protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
}
