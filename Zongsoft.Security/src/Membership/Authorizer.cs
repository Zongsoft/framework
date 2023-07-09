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

		#region 构造函数
		public Authorizer() { }
		#endregion

		#region 公共属性
		[ServiceDependency("@", IsRequired = true)]
		public IDataAccess DataAccess { get; set; }
		#endregion

		#region 公共方法
		public bool Authorize(uint userId, string target, string action)
		{
			if(userId == 0)
				userId = ApplicationContext.Current.Principal.Identity.GetIdentifier<uint>();

			return this.Authorizes(userId, MemberType.User)
			           .Any(token => string.Equals(target, token.Target, StringComparison.OrdinalIgnoreCase) && token.HasAction(action));
		}

		public bool Authorize(ClaimsIdentity user, string target, string action)
		{
			if(user == null)
				throw new ArgumentNullException(nameof(user));

			if(string.IsNullOrEmpty(target))
				throw new ArgumentNullException(nameof(target));

			//创建授权上下文对象
			var context = new AuthorizationContext(true, new AuthorizationArgument(user, target, action));

			//激发“Authorizing”事件
			this.OnAuthorizing(context);

			//如果时间参数指定的验证结果为失败，则直接返回失败
			if(!context.Result)
				return false;

			//如果指定的用户属于系统内置的管理员角色则立即返回授权通过
			if(this.InRoles(user.GetIdentifier<uint>(), IRoleModel.Administrators))
				return true;

			//获取指定的安全凭证对应的有效的授权状态集
			var tokens = this.Authorizes(user.GetIdentifier<uint>(), MemberType.User);

			if(string.IsNullOrWhiteSpace(action) || action == "*")
				context.Result = tokens != null && tokens.Any(state => string.Equals(state.Target, target, StringComparison.OrdinalIgnoreCase));
			else
				context.Result = tokens != null && tokens.Any(
					token => string.Equals(token.Target, target, StringComparison.OrdinalIgnoreCase) &&
							 token.Actions.Any(a => string.Equals(a.Name, action, StringComparison.OrdinalIgnoreCase))
					);

			//激发“Authorized”事件
			this.OnAuthorized(context);

			//返回最终的验证结果
			return context.Result;
		}

		public IEnumerable<Permission> Authorizes(ClaimsIdentity identity)
		{
			if(identity == null)
				throw new ArgumentNullException(nameof(identity));

			return MembershipUtility.GetAuthorizes(this.DataAccess, identity);
		}

		public IEnumerable<Permission> Authorizes(IRoleModel role)
		{
			if(role == null)
				throw new ArgumentNullException(nameof(role));

			return MembershipUtility.GetAuthorizes(this.DataAccess, role);
		}

		public IEnumerable<Permission> Authorizes(uint memberId, MemberType memberType)
		{
			if(memberType == MemberType.User)
			{
				//获取指定编号的用户对象
				var user = this.GetUser(memberId);
				return user == null ? Array.Empty<Permission>() : MembershipUtility.GetAuthorizes(this.DataAccess, user);
			}
			else
			{
				//获取指定编号的角色对象
				var role = this.GetRole(memberId);
				return role == null ? Array.Empty<Permission>() : MembershipUtility.GetAuthorizes(this.DataAccess, role);
			}
		}

		public bool InRoles(uint userId, params string[] roleNames)
		{
			if(roleNames == null || roleNames.Length < 1)
				return false;

			return MembershipUtility.InRoles(
				this.DataAccess,
				this.GetUser(userId, "UserId, Name, Namespace"),
				roleNames);
		}

		public bool InRoles(IUserIdentity user, params string[] roleNames)
		{
			return MembershipUtility.InRoles(this.DataAccess, user as IUserModel, roleNames);
		}
		#endregion

		#region 虚拟方法
		protected virtual IUserModel GetUser(uint userId, string schema = null)
		{
			var fetcher = ApplicationContext.Current.Services.Resolve<IUserFetcher>();

			if(fetcher != null)
				return fetcher.GetUser(userId, schema);

			return this.DataAccess.Select<IUserModel>(Mapping.Instance.User, Condition.Equal(nameof(IUserModel.UserId), userId), schema).FirstOrDefault();
		}

		protected virtual IRoleModel GetRole(uint roleId, string schema = null)
		{
			var fetcher = ApplicationContext.Current.Services.Resolve<IRoleFetcher>();

			if(fetcher != null)
				return fetcher.GetRole(roleId, schema);

			return this.DataAccess.Select<IRoleModel>(Mapping.Instance.Role, Condition.Equal(nameof(IRoleModel.RoleId), roleId), schema).FirstOrDefault();
		}
		#endregion

		#region 事件激发
		protected virtual void OnAuthorizing(AuthorizationContext context) => this.Authorizing?.Invoke(this, context);
		protected virtual void OnAuthorized(AuthorizationContext context) => this.Authorized?.Invoke(this, context);
		#endregion
	}
}
