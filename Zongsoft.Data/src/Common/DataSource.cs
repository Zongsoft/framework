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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

public class DataSource : IDataSource, IEquatable<DataSource>, IEquatable<IDataSource>
{
	#region 公共常量
	public const char SEPARATOR = '#';
	#endregion

	#region 成员字段
	private string _name;
	private string _connectionString;
	private string _driverName;
	private IDataDriver _driver;
	private FeatureCollection _features;
	private readonly Dictionary<string, DataTable> _schemas;
	private readonly SemaphoreSlim _schemaSemaphore = new(1, 1);
	#endregion

	#region 构造函数
	public DataSource(Configuration.IConnectionSettings connectionSettings)
	{
		if(connectionSettings == null)
			throw new ArgumentNullException(nameof(connectionSettings));

		if(connectionSettings.Driver == null || string.IsNullOrWhiteSpace(connectionSettings.Driver.Name))
			throw new DataException(Zongsoft.Data.Properties.Resources.DataSource_DriverUnspecified_Message);

		_name = connectionSettings.Name;
		_connectionString = connectionSettings.Value;
		_driverName = connectionSettings.Driver.Name;
		this.Mode = DataAccessMode.All;
		_schemas = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
		this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		if(connectionSettings.HasProperties)
		{
			if(connectionSettings.Properties.TryGetValue("mode", out var mode) && mode != null && mode.Length > 0)
			{
				this.Mode = mode.Trim().ToLowerInvariant() switch
				{
					"r" or "read" or "readonly" => DataAccessMode.ReadOnly,
					"w" or "write" or "writeonly" => DataAccessMode.WriteOnly,
					"*" or "all" or "both" or "read+write" or "write+read" => DataAccessMode.All,
					_ => throw new Configuration.ConfigurationException(string.Format(Zongsoft.Data.Properties.Resources.DataSource_InvalidAccessMode_Message, mode)),
				};
			}

			foreach(var property in connectionSettings.Properties)
			{
				if(!string.Equals(property.Key, nameof(this.Mode), StringComparison.OrdinalIgnoreCase))
					this.Properties[property.Key] = property.Value;
			}
		}
	}

	public DataSource(string name, string connectionString, string driverName = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(string.IsNullOrEmpty(connectionString))
			throw new ArgumentNullException(nameof(connectionString));

		_name = name;
		_connectionString = connectionString;
		_driverName = driverName;
		this.Mode = DataAccessMode.All;
		_schemas = new Dictionary<string, DataTable>(StringComparer.OrdinalIgnoreCase);
		this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	public string Name => _name;
	public string ConnectionString => _connectionString;
	public DataAccessMode Mode { get; set; }
	public IDictionary<string, object> Properties { get; }

	public IDataDriver Driver
	{
		get
		{
			if(_driver == null && !string.IsNullOrEmpty(_driverName))
				_driver = DataEnvironment.Drivers.TryGetValue(_driverName, out var driver) ? driver : throw new DataException(string.Format(Zongsoft.Data.Properties.Resources.DataSource_DriverNotFound_Message, _driverName));

			return _driver;
		}
	}

	public FeatureCollection Features => _features ??= new FeatureCollection(this.Driver?.Features);
	#endregion

	#region 公共方法
	public ValueTask<DataTable> GetSchemaAsync(string name, CancellationToken cancellation = default) => this.GetSchemaAsync(name, false, cancellation);
	public async ValueTask<DataTable> GetSchemaAsync(string name, bool refresh, CancellationToken cancellation = default)
	{
		name ??= string.Empty;
		await _schemaSemaphore.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			if(!refresh && _schemas.TryGetValue(name, out var cached))
				return cached;

			var schema = await GetSchemaFromDatabaseAsync(name, cancellation).ConfigureAwait(false);
			if(schema != null)
				_schemas[name] = schema;

			return schema;
		}
		finally
		{
			_schemaSemaphore.Release();
		}

		async ValueTask<DataTable> GetSchemaFromDatabaseAsync(string name, CancellationToken cancellation)
		{
			DbConnection connection = null;

			try
			{
				connection = await DataConnectorManager.GetConnector(this).ConnectAsync(cancellation).ConfigureAwait(false);
				return string.IsNullOrEmpty(name) ?
					await connection.GetSchemaAsync(cancellation).ConfigureAwait(false) :
					await connection.GetSchemaAsync(name, cancellation).ConfigureAwait(false);
			}
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
			{
				throw;
			}
			catch { return null; }
			finally
			{
				if(connection != null)
					await connection.DisposeAsync().ConfigureAwait(false);
			}
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(IDataSource other) => other is not null && string.Equals(_name, other.Name, StringComparison.OrdinalIgnoreCase) && this.Mode == other.Mode;
	public bool Equals(DataSource other) => other is not null && string.Equals(_name, other.Name, StringComparison.OrdinalIgnoreCase) && this.Mode == other.Mode;
	public override bool Equals(object obj) => obj is IDataSource other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(_name.ToLowerInvariant(), this.Mode);
	public override string ToString() => string.IsNullOrEmpty(_driverName) ?
		$"{_name} <{_connectionString}>" :
		$"[{_driverName}]{_name} <{_connectionString}>";
	#endregion
}
