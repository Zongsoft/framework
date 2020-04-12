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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Collections.Generic;

namespace Zongsoft.Configuration
{
	public class Setting : ISetting
	{
		#region 成员字段
		private volatile string _name;
		private IDictionary<string, string> _properties;
		#endregion

		#region 构造函数
		public Setting()
		{
		}

		public Setting(string name, string value = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			this.Value = value;
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get => _name;
			set
			{
				if(!string.IsNullOrEmpty(_name))
					throw new InvalidOperationException(Zongsoft.Properties.Resources.Error_RepeatedOperation);

				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		public string Value
		{
			get; set;
		}

		public bool HasProperties
		{
			get => _properties?.Count > 0;
		}

		public IDictionary<string, string> Properties
		{
			get
			{
				if(_properties == null)
					Interlocked.CompareExchange(ref _properties, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null);

				return _properties;
			}
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return $"{this.Name}={this.Value}";
		}
		#endregion
	}
}
