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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public abstract partial class UserServiceBase<TUser> : IUserService<TUser>, IUserService, IMatchable, IMatchable<ClaimsPrincipal> where TUser : IUser
{
	#region 成员字段
	private ISecretor _secretor;
	#endregion

	#region 构造函数
	protected UserServiceBase(Passworder passworder = null) => this.Passworder = passworder;
	#endregion

	#region 公共属性
	public virtual string Name => Model.Naming.Get<TUser>();
	public Passworder Passworder { get; protected set; }
	public ISecretor Secretor
	{
		get => _secretor ??= this.Services.Resolve<ISecretor>();
		set => _secretor = value;
	}
	#endregion

	#region 保护属性
	protected abstract IDataAccess Accessor { get; }
	protected virtual IServiceProvider Services => ApplicationContext.Current?.Services;
	protected virtual ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
	#endregion

	#region 公共方法
	public ValueTask<TUser> GetAsync(Identifier identifier, CancellationToken cancellation = default) => this.GetAsync(identifier, null, cancellation);
	public virtual async ValueTask<TUser> GetAsync(Identifier identifier, string schema, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return default;

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return default;

		return await this.Accessor.SelectAsync<TUser>(criteria, schema, cancellation).FirstOrDefault(cancellation);
	}

	public IAsyncEnumerable<TUser> FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation = default) => this.FindAsync(this.GetCriteria(keyword), schema, paging, cancellation);
	public virtual IAsyncEnumerable<TUser> FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) => this.Accessor.SelectAsync<TUser>(criteria, schema, paging, cancellation);

	public virtual ValueTask<bool> ExistsAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return ValueTask.FromResult(false);

		return this.Accessor.ExistsAsync(this.Name, criteria, cancellation: cancellation);
	}

	public virtual async ValueTask<bool> EnableAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		//确保不能设置内置用户
		var criteria = this.GetCriteria(identifier) & Condition.NotEqual(nameof(IUser.Name), IUser.Administrator);
		return criteria != null && await this.Accessor.UpdateAsync(this.Name, new { Enabled = true }, criteria, cancellation) > 0;
	}

	public virtual async ValueTask<bool> DisableAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		//确保不能禁用内置用户
		var criteria = this.GetCriteria(identifier) & Condition.NotEqual(nameof(IUser.Name), IUser.Administrator);
		return criteria != null && await this.Accessor.UpdateAsync(this.Name, new { Enabled = false }, criteria, cancellation) > 0;
	}

	public virtual async ValueTask<bool> RenameAsync(Identifier identifier, string name, CancellationToken cancellation = default)
	{
		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		//验证指定的名称是否合法
		this.OnValidateName(name);

		//确保不能更名内置用户
		var criteria = this.GetCriteria(identifier) & Condition.NotEqual(nameof(IUser.Name), IUser.Administrator);
		if(criteria == null)
			return false;

		return await this.Accessor.UpdateAsync(this.Name, new { Name = name }, criteria, cancellation) > 0;
	}

	public virtual async ValueTask<bool> SetEmailAsync(Identifier identifier, string email, CancellationToken cancellation = default)
	{
		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return false;

		return await this.Accessor.UpdateAsync(this.Name, new
		{
			Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
		}, criteria, cancellation) > 0;
	}

	public virtual ValueTask<bool> SetEmailAsync(string token, string secret, CancellationToken cancellation) => this.VerifyAsync("email", token, secret, cancellation);
	public virtual async ValueTask<string> SetEmailAsync(Identifier identifier, string email, Parameters parameters, CancellationToken cancellation = default)
	{
		const string SCHEME = "email";

		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		//确保指定用户是存在的
		if(await this.ExistsAsync(identifier, cancellation))
			return await this.Secretor.Transmitter.TransmitAsync(
				SCHEME,
				email,
				GetTemplate(parameters),
				GetScenario(parameters),
				GetCaptcha(parameters),
				GetChannel(parameters),
				$"{SCHEME}:{identifier.Value}|{email}", cancellation);

		return null;

		static string GetTemplate(Parameters parameters) => parameters.TryGetValue("template", out var value) && value is string text ? text : "User.Email";
		static string GetScenario(Parameters parameters) => parameters.TryGetValue("scenario", out var value) && value is string text ? text : null;
		static string GetCaptcha(Parameters parameters) => parameters.TryGetValue("captcha", out var value) && value is string text ? text : null;
		static string GetChannel(Parameters parameters) => parameters.TryGetValue("channel", out var value) && value is string text ? text : null;
	}

	public virtual async ValueTask<bool> SetPhoneAsync(Identifier identifier, string phone, CancellationToken cancellation = default)
	{
		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		var criteria = this.GetCriteria(identifier);
		if(criteria == null)
			return false;

		return await this.Accessor.UpdateAsync(this.Name, new
		{
			Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
		}, criteria, cancellation) > 0;
	}

	public virtual ValueTask<bool> SetPhoneAsync(string token, string secret, CancellationToken cancellation) => this.VerifyAsync("phone", token, secret, cancellation);
	public virtual async ValueTask<string> SetPhoneAsync(Identifier identifier, string phone, Parameters parameters, CancellationToken cancellation = default)
	{
		const string SCHEME = "phone";

		//确认指定的用户标识是否有效
		identifier = EnsureIdentity(identifier);

		//确保指定用户是存在的
		if(await this.ExistsAsync(identifier, cancellation))
			return await this.Secretor.Transmitter.TransmitAsync(
				SCHEME,
				phone,
				GetTemplate(parameters),
				GetScenario(parameters),
				GetCaptcha(parameters),
				GetChannel(parameters),
				$"{SCHEME}:{identifier.Value}|{phone}", cancellation);

		return null;

		static string GetTemplate(Parameters parameters) => parameters.TryGetValue("template", out var value) && value is string text ? text : "User.Email";
		static string GetScenario(Parameters parameters) => parameters.TryGetValue("scenario", out var value) && value is string text ? text : null;
		static string GetCaptcha(Parameters parameters) => parameters.TryGetValue("captcha", out var value) && value is string text ? text : null;
		static string GetChannel(Parameters parameters) => parameters.TryGetValue("channel", out var value) && value is string text ? text : null;
	}

	public ValueTask<bool> CreateAsync(TUser user, CancellationToken cancellation = default) => this.CreateAsync(user, null, cancellation);
	public virtual async ValueTask<bool> CreateAsync(TUser user, string password, CancellationToken cancellation = default)
	{
		if(user == null)
			return false;

		//确认待创建的用户实体
		this.OnCreating(user);

		//验证指定的名称是否合法
		this.OnValidateName(user.Name);

		//确认新密码是否符合密码规则
		this.OnValidatePassword(password);

		if(await this.Accessor.InsertAsync(user, cancellation) > 0)
		{
			//有效的密码不能为空或全空格字符串
			if(!string.IsNullOrWhiteSpace(password))
				await this.Passworder.SetAsync(user.Identifier, password, cancellation);

			//通知用户创建完成
			this.OnCreated(user);

			//返回成功
			return true;
		}

		//返回失败
		return false;
	}

	public virtual async ValueTask<int> CreateAsync(IEnumerable<TUser> users, CancellationToken cancellation = default)
	{
		if(users == null)
			return 0;

		foreach(var user in users)
		{
			if(user == null)
				continue;

			//确认待创建的用户实体
			this.OnCreating(user);

			//确认待创建的用户实体
			this.OnCreating(user);

			//验证指定的名称是否合法
			this.OnValidateName(user.Name);
		}

		var count = await this.Accessor.InsertAsync(users, cancellation);

		if(count > 0)
		{
			foreach(var user in users)
			{
				if(user != null && user.Identifier.HasValue)
					this.OnCreated(user);
			}
		}

		return count;
	}

	public virtual async ValueTask<bool> DeleteAsync(Identifier identifier, CancellationToken cancellation = default)
	{
		if(identifier.IsEmpty)
			return false;

		//确保不能删除内置用户
		var criteria = this.GetCriteria(identifier) & Condition.NotEqual(nameof(IUser.Name), IUser.Administrator);
		if(criteria == null)
			return false;

		return await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation) > 0;
	}

	public virtual async ValueTask<int> DeleteAsync(IEnumerable<Identifier> identifiers, CancellationToken cancellation = default)
	{
		if(identifiers == null)
			return 0;

		//删除数量
		var total = 0;

		//创建事务
		using var transaction = new Zongsoft.Transactions.Transaction();

		foreach(var identifier in identifiers)
		{
			if(identifier.IsEmpty)
				continue;

			//确保不能禁用内置用户
			var criteria = this.GetCriteria(identifier) & Condition.NotEqual(nameof(IUser.Name), IUser.Administrator);
			if(criteria == null)
				continue;

			var count = await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);
			if(count > 0)
			{
				total += count;

				//删除成员表中当前用户的所有父级
				await Authentication.Servicer.Members.RemoveAsync(Member.User(identifier), cancellation);
			}
		}

		//提交事务
		transaction.Commit();

		//返回删除数量
		return total;
	}

	public virtual async ValueTask<bool> UpdateAsync(TUser user, CancellationToken cancellation = default)
	{
		if(user == null)
			return false;

		//验证指定的名称是否合法
		this.OnValidateName(user.Name);

		//确保修改的不是内置用户
		var criteria = this.GetCriteria(user.Identifier) & Condition.NotEqual(nameof(IUser.Name), IUser.Administrator);
		if(!await this.Accessor.ExistsAsync(this.Name, criteria, cancellation: cancellation))
			return false;

		return await this.Accessor.UpdateAsync(user, cancellation) > 0;
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnCreated(TUser user) { }
	protected virtual void OnCreating(TUser user)
	{
		if(string.IsNullOrWhiteSpace(user.Name))
		{
			if(string.IsNullOrWhiteSpace(user.Phone) && string.IsNullOrWhiteSpace(user.Email))
				throw new ArgumentException("The user name is empty.");

			//虽然用户名为空但是指定了绑定的“Phone”或“Email”，则将用户名设置为随机值
			user.Name = "$U" + Randomizer.GenerateString();
		}

		if(string.IsNullOrWhiteSpace(user.Namespace))
			user.Namespace = null;
	}

	protected virtual void OnValidateName(string name)
	{
		//验证指定的名称是否为系统内置名
		if(string.Equals(name, IUser.Administrator, StringComparison.OrdinalIgnoreCase))
			throw new SecurityException("username.illegality", "The user name specified to be update cannot be a built-in name.");

		var validator = this.Services?.Find<IValidator<string>>("user.name");
		validator?.Validate(name, message => throw new SecurityException("username.illegality", message));
	}

	protected virtual ICondition GetCriteria(string keyword)
	{
		if(string.IsNullOrEmpty(keyword) || keyword == "*")
			return null;

		var criteria = UserUtility.GetCriteria(keyword, out var identityType);
		return string.Equals(identityType, nameof(IUser.Name)) ?
			criteria | Condition.Like(nameof(IUser.Nickname), keyword) : criteria;
	}

	protected virtual ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.IsEmpty)
			return null;

		if(identifier.Validate(out string qualifiedName))
			return UserUtility.GetCriteria(qualifiedName);

		throw OperationException.Argument();
	}

	protected virtual ICondition GetCriteria(string identity, string @namespace) => UserUtility.GetCriteria(identity, @namespace);
	protected virtual ICondition GetCriteria(string identity, string @namespace, out string identityType) => UserUtility.GetCriteria(identity, @namespace, out identityType);
	#endregion

	#region 秘密校验
	private async ValueTask<bool> VerifyAsync(string type, string token, string secret, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(type))
			throw new ArgumentNullException(nameof(type));

		if(string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
			return false;

		//校验指定的密文
		(var succeed, var extra) = await this.Secretor.VerifyAsync(token, secret, cancellation);

		//如果校验成功并且密文中有附加数据
		if(succeed && (extra != null && extra.Length > 0))
		{
			var index = extra.IndexOf(':');
			if(index < 1 || !string.Equals(type, extra[..index], StringComparison.OrdinalIgnoreCase))
				return false;

			var parts = extra[(index + 1)..].Split('|');
			if(parts.Length < 2)
				return false;

			return type switch
			{
				"email" => await this.SetEmailAsync(new Identifier(typeof(TUser), parts[0]), parts[1], cancellation),
				"phone" => await this.SetPhoneAsync(new Identifier(typeof(TUser), parts[0]), parts[1], cancellation),
				_ => false,
			};
		}

		return false;
	}
	#endregion

	#region 私有方法
	private static Identifier EnsureIdentity(Identifier identifier)
	{
		if(identifier.IsEmpty)
			return new Identifier(typeof(TUser), ApplicationContext.Current.Principal.Identity.GetIdentifier());

		/*
		 * 只有当前用户是如下情况之一，才能操作指定的其他用户：
		 *   1) 指定的用户就是当前用户自己；
		 *   2) 当前用户是系统管理员(Administrators)或安全管理员角色(Security)成员。
		 */

		var current = ApplicationContext.Current.Principal.Identity.GetIdentifier();

		if(object.Equals(current, identifier.Value) || ApplicationContext.Current.Principal.InRoles([IRole.Administrators, IRole.Security]))
			return identifier;

		throw new AuthorizationException($"The current user cannot operate on other user information.");
	}
	#endregion

	#region 显式实现
	async ValueTask<IUser> IUserService.GetAsync(Identifier identifier, CancellationToken cancellation) => await this.GetAsync(identifier, cancellation);
	async ValueTask<IUser> IUserService.GetAsync(Identifier identifier, string schema, CancellationToken cancellation) => await this.GetAsync(identifier, schema, cancellation);
	IAsyncEnumerable<IUser> IUserService.FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.FindAsync(keyword, schema, paging, cancellation).Map(user => (IUser)user);
	IAsyncEnumerable<IUser> IUserService.FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.FindAsync(criteria, schema, paging, cancellation).Map(user => (IUser)user);
	ValueTask<bool> IUserService.CreateAsync(IUser user, CancellationToken cancellation) => this.CreateAsync(user is TUser model ? model : default, cancellation);
	ValueTask<bool> IUserService.CreateAsync(IUser user, string password, CancellationToken cancellation) => this.CreateAsync(user is TUser model ? model : default, password, cancellation);
	ValueTask<int> IUserService.CreateAsync(IEnumerable<IUser> users, CancellationToken cancellation) => this.CreateAsync(users.Cast<TUser>(), cancellation);
	ValueTask<bool> IUserService.UpdateAsync(IUser user, CancellationToken cancellation) => this.UpdateAsync(user is TUser model ? model : default, cancellation);
	#endregion

	#region 服务匹配
	bool IMatchable.Match(object argument) => argument is ClaimsPrincipal principal && this.OnMatch(principal);
	bool IMatchable<ClaimsPrincipal>.Match(ClaimsPrincipal argument) => this.OnMatch(argument);
	protected virtual bool OnMatch(ClaimsPrincipal principal) => principal != null && principal.Identity != null && principal.Identity.IsAuthenticated;
	#endregion
}
