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
using System.Collections.ObjectModel;
using System.Collections.Specialized;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 提供身份验证的平台类。
	/// </summary>
	[System.Reflection.DefaultMember(nameof(Authenticators))]
	public class Authentication
	{
		#region 静态变量
		private static DateTime EPOCH = new DateTime(2000, 1, 1);
		#endregion

		#region 单例字段
		public static readonly Authentication Instance = new Authentication();
		#endregion

		#region 构造函数
		private Authentication()
		{
			var authenticators = new ObservableCollection<IAuthenticator>();
			authenticators.CollectionChanged += OnCollectionChanged;

			this.Authenticators = authenticators;
			this.Filters = new List<IExecutionFilter>();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置凭证主体的提供程序。
		/// </summary>
		public ICredentialProvider Authority { get; set; }

		/// <summary>
		/// 获取身份验证器的集合。
		/// </summary>
		public ICollection<IAuthenticator> Authenticators { get; }

		/// <summary>
		/// 获取一个身份验证的过滤器集合，该过滤器包含对身份验证的响应处理。
		/// </summary>
		public ICollection<IExecutionFilter> Filters { get; }

		/// <summary>
		/// 获取或设置命名空间映射器。
		/// </summary>
		public INamespaceMapper Namespaces { get; set; }
		#endregion

		#region 公共方法
		public CredentialPrincipal Authenticate(string identity, string password, string @namespace, string scenario, ref IDictionary<string, object> parameters)
		{
			var identities = new List<ClaimsIdentity>(this.Authenticators.Count);

			foreach(var authenticator in this.Authenticators)
			{
				identities.Add(authenticator.Authenticate(identity, password, @namespace, scenario, ref parameters));
			}

			var principal = new CredentialPrincipal(GenerateId(out var token), token, scenario, identities);

			foreach(var filter in this.Filters)
			{
				filter.OnFiltered(principal);
			}

			this.Authority.Register(principal);

			return principal;
		}

		public CredentialPrincipal AuthenticateSecret(string identity, string secret, string @namespace, string scenario, ref IDictionary<string, object> parameters)
		{
			var identities = new List<ClaimsIdentity>(this.Authenticators.Count);

			foreach(var authenticator in this.Authenticators)
			{
				identities.Add(authenticator.AuthenticateSecret(identity, secret, @namespace, scenario, ref parameters));
			}

			var principal = new CredentialPrincipal(GenerateId(out var token), token, scenario, identities);

			foreach(var filter in this.Filters)
			{
				filter.OnFiltered(principal);
			}

			return principal;
		}
		#endregion

		#region 静态方法
		public static string GenerateId()
		{
			return ((ulong)(DateTime.UtcNow - EPOCH).TotalSeconds).ToString() + Randomizer.GenerateString(8);
		}

		public static string GenerateId(out string token)
		{
			token = ((ulong)(DateTime.UtcNow - EPOCH).TotalDays).ToString() + Environment.TickCount64.ToString("X") + Randomizer.GenerateString(8);
			return GenerateId();
		}
		#endregion

		#region 事件响应
		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			switch(args.Action)
			{
				case NotifyCollectionChangedAction.Add:
					for(int i=args.NewStartingIndex; i< args.NewItems.Count; i++)
					{
						((IAuthenticator)args.NewItems[i]).Authenticated += OnAuthenticated;
						((IAuthenticator)args.NewItems[i]).Authenticating += OnAuthenticating;
					}

					break;
				case NotifyCollectionChangedAction.Reset:
				case NotifyCollectionChangedAction.Remove:
					for(int i = args.OldStartingIndex; i < args.OldItems.Count; i++)
					{
						((IAuthenticator)args.OldItems[i]).Authenticated -= OnAuthenticated;
						((IAuthenticator)args.OldItems[i]).Authenticating -= OnAuthenticating;
					}

					break;
				case NotifyCollectionChangedAction.Replace:
					for(int i = args.OldStartingIndex; i < args.OldItems.Count; i++)
					{
						((IAuthenticator)args.OldItems[i]).Authenticated -= OnAuthenticated;
						((IAuthenticator)args.OldItems[i]).Authenticating -= OnAuthenticating;
					}

					for(int i = args.NewStartingIndex; i < args.NewItems.Count; i++)
					{
						((IAuthenticator)args.NewItems[i]).Authenticated += OnAuthenticated;
						((IAuthenticator)args.NewItems[i]).Authenticating += OnAuthenticating;
					}

					break;
			}
		}

		private void OnAuthenticating(object sender, AuthenticatingEventArgs args)
		{
			foreach(var filter in this.Filters)
			{
				filter.OnFiltering(args);
			}
		}

		private void OnAuthenticated(object sender, AuthenticatedEventArgs args)
		{
			foreach(var filter in this.Filters)
			{
				filter.OnFiltered(args);
			}
		}
		#endregion
	}
}
