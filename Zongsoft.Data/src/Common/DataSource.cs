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
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Common
{
	public class DataSource : IDataSource, IEquatable<DataSource>, IEquatable<IDataSource>
	{
		#region 公共常量
		public const char SEPARATOR = '#';
		#endregion

		#region 私有常量
		private static readonly Regex MARS_FEATURE = new Regex(@"\bMultipleActiveResultSets\s*=\s*True\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 成员字段
		private string _name;
		private string _connectionString;
		private string _driverName;
		private IDataDriver _driver;
		private FeatureCollection _features;
		#endregion

		#region 构造函数
		public DataSource(Configuration.IConnectionSetting connectionSetting)
		{
			if(connectionSetting == null)
				throw new ArgumentNullException(nameof(connectionSetting));

			if(string.IsNullOrWhiteSpace(connectionSetting.Driver))
				throw new DataException($"Missing driver in the data connection string.");

			_name = connectionSetting.Name;
			_connectionString = connectionSetting.Value;
			_driverName = connectionSetting.Driver;
			this.Mode = DataAccessMode.All;
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

			if(connectionSetting.HasProperties)
			{
				if(connectionSetting.Properties.TryGetValue("mode", out var mode) && mode != null && mode.Length > 0)
				{
					this.Mode = mode.Trim().ToLowerInvariant() switch
					{
						"r" or "read" or "readonly" => DataAccessMode.ReadOnly,
						"w" or "write" or "writeonly" => DataAccessMode.WriteOnly,
						"*" or "all" or "none" or "both" or "read+write" or "write+read" => DataAccessMode.All,
						_ => throw new Configuration.ConfigurationException($"Invalid '{mode}' mode value of the ConnectionString configuration."),
					};
				}

				foreach(var property in connectionSetting.Properties)
				{
					if(!string.Equals(property.Key, nameof(Mode), StringComparison.OrdinalIgnoreCase))
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
					_driver = DataEnvironment.Drivers.TryGetValue(_driverName, out var driver) ? driver : throw new DataException($"The '{_driverName}' data driver does not exist.");

				return _driver;
			}
		}

		public FeatureCollection Features
		{
			get
			{
				if(_features == null)
				{
					_features = new FeatureCollection(this.Driver?.Features);

					if(!string.IsNullOrEmpty(_connectionString) && MARS_FEATURE.IsMatch(_connectionString))
						_features.Add(Feature.MultipleActiveResultSets);
				}

				return _features;
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
}
