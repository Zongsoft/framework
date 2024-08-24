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
		private IDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public AuthenticatedEventArgs(Authentication authentication, ClaimsPrincipal principal, string scenario, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			this.Authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
			this.Principal = principal ?? throw new ArgumentNullException(nameof(principal));
			this.Scenario = scenario;

			if(parameters != null)
				_parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		/// <summary>获取激发的身份验证对象。</summary>
		public Authentication Authentication { get; }

		/// <summary>获取身份验证的用户身份。</summary>
		public ClaimsPrincipal Principal { get; }

		/// <summary>获取身份验证的应用场景。</summary>
		public string Scenario { get; }

		/// <summary>获取身份验证是否通过。</summary>
		public bool IsAuthenticated
		{
			get => Principal != null && this.Principal.Identity != null &&
				Principal.Identity.IsAuthenticated && !string.IsNullOrEmpty(Principal.Identity.Name);
		}

		/// <summary>获取一个值，指示扩展参数集是否有内容。</summary>
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		/// <summary>获取验证结果的扩展参数集。</summary>
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
		public AuthenticatingEventArgs(Authentication authentication, object ticket, string scenario, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			this.Authentication = authentication ?? throw new ArgumentNullException(nameof(authentication));
			this.Ticket = ticket;
			this.Scenario = scenario;

			if(parameters != null)
				_parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		/// <summary>获取激发的身份验证对象。</summary>
		public Authentication Authentication { get; }

		/// <summary>获取待验证的票证对象。</summary>
		public object Ticket { get; }

		/// <summary>获取身份验证的应用场景。</summary>
		public string Scenario { get; }

		/// <summary>获取一个值，指示扩展参数集是否有内容。</summary>
		public bool HasParameters
		{
			get => _parameters != null && _parameters.Count > 0;
		}

		/// <summary>获取验证结果的扩展参数集。</summary>
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
