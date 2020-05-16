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
using System.Security.Claims;

namespace Zongsoft.Security.Membership
{
	public class AuthenticationContext
	{
		#region 成员字段
		private AuthenticationResult _result;
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public AuthenticationContext(string scenario, IDictionary<string, object> parameters)
		{
			this.Scenario = scenario;

			if(parameters != null && parameters.Count > 0)
				_parameters = new Dictionary<string, object>(parameters);
		}
		#endregion

		#region 公共属性
		public string Scenario { get; }

		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		public IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}

		public ClaimsPrincipal Principal { get => _result?.Principal; }

		public bool Succeed { get => _result?.Succeed == true; }

		public bool Failed { get => _result?.Failed == true; }

		public AuthenticationResult Result
		{
			get => _result;
			set => _result = value ?? throw new ArgumentNullException();
		}
		#endregion
	}
}
