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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zongsoft.Services
{
	public class ServiceResolvingEventArgs : System.ComponentModel.CancelEventArgs
	{
		#region 成员字段
		private string _serviceName;
		private Type _contractType;
		private object _parameter;
		private bool _isResolveAll;
		private object _result;
		#endregion

		#region 构造函数
		public ServiceResolvingEventArgs(string serviceName)
		{
			if(string.IsNullOrWhiteSpace(serviceName))
				throw new ArgumentNullException("serviceName");

			_serviceName = serviceName.Trim();
			_isResolveAll = false;
		}

		public ServiceResolvingEventArgs(Type contractType, object parameter, bool isResolveAll)
		{
			if(contractType == null)
				throw new ArgumentNullException("contractType");

			_contractType = contractType;
			_parameter = parameter;
			_isResolveAll = isResolveAll;
		}
		#endregion

		#region 公共属性
		public string ServiceName
		{
			get
			{
				return _serviceName;
			}
		}

		public Type ContractType
		{
			get
			{
				return _contractType;
			}
		}

		public object Parameter
		{
			get
			{
				return _parameter;
			}
		}

		public bool IsResolveAll
		{
			get
			{
				return _isResolveAll;
			}
		}

		public object Result
		{
			get
			{
				return _result;
			}
			set
			{
				_result = value;
			}
		}
		#endregion
	}
}
