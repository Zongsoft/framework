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
		public AuthenticationContext(string scheme, string scenario, AuthenticationResult result)
		{
			_result = result ?? throw new ArgumentNullException(nameof(result));

			this.Scheme = scheme;
			this.Scenario = scenario;

			if(result.Parameters != null && result.Parameters.Count > 0)
				_parameters = new Dictionary<string, object>(result.Parameters);
		}
		#endregion

		#region 公共属性
		/// <summary>获取验证器的方案名。</summary>
		public string Scheme { get; }

		/// <summary>获取验证的场景名。</summary>
		public string Scenario { get; }

		/// <summary>获取一个值，指示参数集是否有值。</summary>
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		/// <summary>获取参数集。</summary>
		public IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}

		/// <summary>获取当前验证的用户主体。</summary>
		public ClaimsPrincipal Principal { get => _result?.Principal; }

		/// <summary>获取一个值，指示是否验证成功。</summary>
		public bool Succeed { get => _result?.Succeed == true; }

		/// <summary>获取一个值，指示是否验证失败。</summary>
		public bool Failed { get => _result?.Failed == true; }

		/// <summary>获取验证的结果。</summary>
		public AuthenticationResult Result
		{
			get => _result;
			set => _result = value ?? throw new ArgumentNullException();
		}
		#endregion
	}
}
