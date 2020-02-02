/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Common
{
	public class DataSource : IDataSource
	{
		#region 常量定义
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
		public DataSource(Options.Configuration.ConnectionStringElement connectionString)
		{
			if(connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));

			_name = connectionString.Name;
			_connectionString = connectionString.Value;
			_driverName = connectionString.Provider;
			this.Mode = DataAccessMode.All;

			if(connectionString.HasExtendedProperties)
			{
				if(connectionString.ExtendedProperties.TryGetValue("mode", out var mode) && mode != null && mode.Length > 0)
				{
					switch(mode.Trim().ToLowerInvariant())
					{
						case "r":
						case "read":
						case "readonly":
							this.Mode = DataAccessMode.ReadOnly;
							break;
						case "w":
						case "write":
						case "writeonly":
							this.Mode = DataAccessMode.WriteOnly;
							break;
						case "*":
						case "all":
						case "none":
						case "both":
						case "readwrite":
						case "writeread":
							this.Mode = DataAccessMode.All;
							break;
						default:
							throw new Options.Configuration.OptionConfigurationException($"Invalid '{mode}' mode value of the ConnectionString configuration.");
					}
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
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value;
			}
		}

		public string ConnectionString
		{
			get
			{
				return _connectionString;
			}
			set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				//如果连接字符串没有发生改变则返回
				if(string.Equals(_connectionString, value, StringComparison.OrdinalIgnoreCase))
					return;

				//更新连接字符串成员字段
				_connectionString = value;

				//重新设置多活动结果集特性
				if(_features != null && MARS_FEATURE.IsMatch(_connectionString))
					_features.Add(Feature.MultipleActiveResultSets);
			}
		}

		public DataAccessMode Mode
		{
			get;
			set;
		}

		public IDataDriver Driver
		{
			get
			{
				if(_driver == null && _driverName != null && _driverName.Length > 0)
				{
					if(!DataEnvironment.Drivers.TryGet(_driverName, out _driver))
						throw new DataException($"The '{_driverName}' data driver does not exist.");
				}

				return _driver;
			}
		}

		public FeatureCollection Features
		{
			get
			{
				if(_features == null)
				{
					_features = new FeatureCollection(this.Driver.Features);

					if(!string.IsNullOrEmpty(_connectionString) && MARS_FEATURE.IsMatch(_connectionString))
						_features.Add(Feature.MultipleActiveResultSets);
				}

				return _features;
			}
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return $"[{_driverName}]{_name} ({_connectionString})";
		}
		#endregion
	}
}
