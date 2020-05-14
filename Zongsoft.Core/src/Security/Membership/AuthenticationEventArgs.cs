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
using System.Security.Claims;
using System.Collections.Generic;

namespace Zongsoft.Security.Membership
{
	public class AuthenticatedEventArgs : EventArgs
	{
		#region 成员字段
		private ClaimsIdentity _identity;
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public AuthenticatedEventArgs(IAuthenticator authenticator, ClaimsIdentity identity, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			this.Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
			this.Identity = identity;

			if(parameters != null)
				_parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取激发的验证器对象。
		/// </summary>
		public IAuthenticator Authenticator
		{
			get;
		}

		/// <summary>
		/// 获取身份验证是否通过。
		/// </summary>
		public bool IsAuthenticated
		{
			get => _identity != null && _identity.IsAuthenticated && !string.IsNullOrEmpty(_identity.Name);
		}

		/// <summary>
		/// 获取身份验证的用户身份。
		/// </summary>
		public ClaimsIdentity Identity
		{
			get => _identity;
			set => _identity = value;
		}

		/// <summary>
		/// 获取一个值，指示扩展参数集是否有内容。
		/// </summary>
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		/// <summary>
		/// 获取验证结果的扩展参数集。
		/// </summary>
		public IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}
		#endregion
	}

	public class AuthenticatingEventArgs : EventArgs
	{
		#region 成员字段
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public AuthenticatingEventArgs(IAuthenticator authenticator, string @namespace, string identity, string scenario, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			this.Authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
			this.Namespace = @namespace;
			this.Identity = identity;
			this.Scenario = scenario;

			if(parameters != null)
				_parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取激发的验证器对象。
		/// </summary>
		public IAuthenticator Authenticator
		{
			get;
		}

		/// <summary>
		/// 获取身份验证的身份标识。
		/// </summary>
		public string Identity
		{
			get;
		}

		/// <summary>
		/// 获取身份验证的命名空间。
		/// </summary>
		public string Namespace
		{
			get;
		}

		/// <summary>
		/// 获取身份验证的应用场景。
		/// </summary>
		public string Scenario
		{
			get;
		}

		/// <summary>
		/// 获取一个值，指示扩展参数集是否有内容。
		/// </summary>
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		/// <summary>
		/// 获取验证结果的扩展参数集。
		/// </summary>
		public IDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}
		#endregion
	}
}
