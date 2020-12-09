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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	[Service(typeof(IAuthorizer))]
	public class Authorizer : IAuthorizer
	{
		#region 事件定义
		public event EventHandler<AuthorizationContext> Authorizing;
		public event EventHandler<AuthorizationContext> Authorized;
		#endregion

		#region 成员字段
		private IDataAccess _dataAccess;
		#endregion

		#region 构造函数
		public Authorizer()
		{
		}
		#endregion

		#region 公共属性
		public IDataAccess DataAccess
		{
			get => _dataAccess ?? (_dataAccess = this.DataAccessProvider.GetAccessor(Modules.Security));
			set => _dataAccess = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IDataAccessProvider DataAccessProvider { get; set; }
		#endregion

		#region 公共方法
		public bool Authorize(uint userId, string schema, string action)
		{
			if(userId == 0)
				userId = ApplicationContext.Current.Principal.Identity.GetIdentifier<uint>();

			return this.Authorizes(userId, MemberType.User)
			           .Any(token => string.Equals(schema, token.Schema, StringComparison.OrdinalIgnoreCase) && token.HasAction(action));
		}

		public bool Authorize(ClaimsIdentity user, string schema, string action)
		{
			if(user == null)
				throw new ArgumentNullException(nameof(user));

			if(string.IsNullOrEmpty(schema))
				throw new ArgumentNullException(nameof(schema));

			//创建授权上下文对象
			var context = new AuthorizationContext(user, schema, action, true);

			//激发“Authorizing”事件
			this.OnAuthorizing(context);

			//如果时间参数指定的验证结果为失败，则直接返回失败
			if(!context.IsAuthorized)
				return false;

			//如果指定的用户属于系统内置的管理员角色则立即返回授权通过
			if(this.InRoles(user.GetIdentifier<uint>(), IRole.Administrators))
				return true;

			//获取指定的安全凭证对应的有效的授权状态集
			var tokens = this.Authorizes(user.GetIdentifier<uint>(), MemberType.User);

			if(string.IsNullOrWhiteSpace(action) || action == "*")
				context.IsAuthorized = tokens != null && tokens.Any(state => string.Equals(state.Schema, schema, StringComparison.OrdinalIgnoreCase));
			else
				context.IsAuthorized = tokens != null && tokens.Any(
					token => string.Equals(token.Schema, schema, StringComparison.OrdinalIgnoreCase) &&
							 token.Actions.Any(p => string.Equals(p.Action, action, StringComparison.OrdinalIgnoreCase))
				);

			//激发“Authorized”事件
			this.OnAuthorized(context);

			//返回最终的验证结果
			return context.IsAuthorized;
		}

		public IEnumerable<AuthorizationToken> Authorizes(ClaimsIdentity user)
		{
			if(user == null)
				throw new ArgumentNullException(nameof(user));

			return this.GetAuthorizedTokens(user.GetNamespace(), user.GetIdentifier<uint>(), MemberType.User);
		}

		public IEnumerable<AuthorizationToken> Authorizes(IRole role)
		{
			if(role == null)
				throw new ArgumentNullException(nameof(role));

			return this.GetAuthorizedTokens(role.Namespace, role.RoleId, MemberType.Role);
		}

		public IEnumerable<AuthorizationToken> Authorizes(uint memberId, MemberType memberType)
		{
			string @namespace;

			if(memberType == MemberType.User)
			{
				//获取指定编号的用户对象
				var user = this.DataAccess.Select<IUser>(Condition.Equal(nameof(IUser.UserId), memberId), "!, UserId, Name, Namespace").FirstOrDefault();

				//如果指定编号的用户不存在，则退出
				if(user == null)
					return Array.Empty<AuthorizationToken>();

				@namespace = user.Namespace;
			}
			else
			{
				//获取指定编号的角色对象
				var role = this.DataAccess.Select<IRole>(Condition.Equal(nameof(IRole.RoleId), memberId), "!, RoleId, Name, Namespace").FirstOrDefault();

				//如果指定编号的角色不存在或是一个内置角色（内置角色没有归属），则退出
				if(role == null)
					return Array.Empty<AuthorizationToken>();

				@namespace = role.Namespace;
			}

			return this.GetAuthorizedTokens(@namespace, memberId, memberType);
		}

		public bool InRoles(uint userId, params string[] roleNames)
		{
			if(roleNames == null || roleNames.Length < 1)
				return false;

			return MembershipUtility.InRoles(
				this.DataAccess,
				this.DataAccess.Select<IUser>(Condition.Equal(nameof(IUser.UserId), userId), "UserId, Name, Namespace").FirstOrDefault(),
				roleNames);
		}

		public bool InRoles(IUserIdentity user, params string[] roleNames)
		{
			return MembershipUtility.InRoles(this.DataAccess, user as IUser, roleNames);
		}
		#endregion

		#region 虚拟方法
		protected virtual IEnumerable<AuthorizationToken> GetAuthorizedTokens(string @namespace, uint memberId, MemberType memberType)
		{
			return MembershipHelper.GetAuthorizedTokens(this.DataAccess, @namespace, memberId, memberType);
		}
		#endregion

		#region 事件激发
		protected virtual void OnAuthorizing(AuthorizationContext context)
		{
			this.Authorizing?.Invoke(this, context);
		}

		protected virtual void OnAuthorized(AuthorizationContext context)
		{
			this.Authorized?.Invoke(this, context);
		}
		#endregion
	}
}
