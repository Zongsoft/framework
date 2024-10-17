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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[Area(Module.NAME)]
	[Authorize]
	[Route("[area]/Users")]
	public abstract class UserControllerBase<TUser> : ControllerBase where TUser : IUserModel
	{
		#region 成员字段
		private IUserProvider<TUser> _userProvider;
		private IAuthorizer _authorizer;
		private IMemberProvider<IRoleModel, IUserModel> _memberProvider;
		private IPermissionProvider _permissionProvider;
		#endregion

		#region 构造函数
		protected UserControllerBase() { }
		#endregion

		#region 公共属性
		[ServiceDependency(IsRequired = true)]
		public IAuthorizer Authorizer
		{
			get => _authorizer;
			set => _authorizer = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IUserProvider<TUser> UserProvider
		{
			get => _userProvider;
			set => _userProvider = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IMemberProvider<IRoleModel, IUserModel> MemberProvider
		{
			get => _memberProvider;
			set => _memberProvider = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IPermissionProvider PermissionProvider
		{
			get => _permissionProvider;
			set => _permissionProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		/// <summary>根据标识获取用户信息。</summary>
		/// <param name="identifier">指定的用户标识，支持用户编号、手机号码、邮箱地址，以及关联的命名空间。</param>
		/// <param name="page">指定的分页信息。</param>
		/// <returns>返回的用户用户信息。</returns>
		/// <remarks>
		/// 参数 <paramref name="identifier"/> 支持的格式及相关语义如下：
		/// <list type="bullet">
		///		<item>
		///			<term>GET /Security/Users/100</term>
		///			<description>表示获取指定<c>用户编号</c>的单条用户信息。</description>
		///		</item>
		///		<item>
		///			<term>GET /Security/Users/{name}</term>
		///			<description>表示获取指定<c>用户名称</c>的用户信息。</description>
		///		</item>
		///		<item>
		///			<term>GET /Security/Users/{namespace}:{name}</term>
		///			<description>表示获取指定<c>命名空间</c>中<c>用户名称</c>的用户信息。</description>
		///		</item>
		///		<item>
		///			<term>GET /Security/Users/{namespace}:{phone}</term>
		///			<description>表示获取指定<c>命名空间</c>中<c>手机号码</c>的用户信息。</description>
		///		</item>
		///		<item>
		///			<term>GET /Security/Users/{namespace}:{email}</term>
		///			<description>表示获取指定<c>命名空间</c>中<c>邮箱地址</c>的用户信息。</description>
		///		</item>
		/// </list>
		/// <para>
		///		由于<c>手机号码</c>与<c>用户编号</c>都是数字，因此在未指定<c>命名空间</c>时<c>手机号码</c>会被当作<c>用户编号</c>处理，可通过如下格式来避免该歧义发生：
		///		<code>GET /Security/Users/:{phone}</code>
		///	</para>
		/// </remarks>
		[HttpGet("{identifier?}")]
		public Task<IActionResult> Get(string identifier, [FromQuery] Paging page = null)
		{
			if(string.IsNullOrEmpty(identifier) || identifier == "*")
				return Task.FromResult(this.Paginate(page ??= Paging.First(), this.UserProvider.GetUsers(identifier, page)));

			if(uint.TryParse(identifier, out var id))
			{
				var user = this.UserProvider.GetUser(id);

				return user != null ?
					Task.FromResult((IActionResult)this.Ok(user)) :
					Task.FromResult((IActionResult)this.NoContent());
			}

			var identity = IdentityQualifier.Parse(identifier);
			var result = this.UserProvider.GetUser(identity.Identity, identity.Namespace);

			return result != null ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}")]
		public Task<IActionResult> Delete(uint id)
		{
			if(id == 0)
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.UserProvider.Delete(id) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete]
		public async Task<IActionResult> DeleteAsync()
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var ids = Common.StringExtension.Slice<uint>(content, new[] { ',', '|' }, uint.TryParse).ToArray();

			if(ids == null || ids.Length == 0)
				return this.BadRequest();

			return this.UserProvider.Delete(ids) > 0 ? this.NoContent() : this.NotFound();
		}

		[AllowAnonymous]
		[HttpPost("Register")]
		public IActionResult Register([FromBody] RegisterEntity entity, [FromQuery] string token)
		{
			var user = this.UserProvider.Register(entity.Namespace, entity.Identity, token, entity.Password, entity.Parameters);
			return user == null ? this.NoContent() : this.Ok(user);
		}

		[HttpPost]
		public ActionResult<TUser> Create([FromBody] TUser model)
		{
			if(model == null)
				return this.BadRequest();

			//从请求消息的头部获取指定的用户密码
			this.Request.Headers.TryGetValue("x-password", out var password);

			if(this.UserProvider.Create(model, password))
				return this.CreatedAtAction(nameof(Get), new { id = model.UserId }, model);

			return this.Conflict();
		}

		[HttpPut("{id?}")]
		[HttpPatch("{id?}")]
		public Task<IActionResult> Update(uint id, [FromBody] TUser model)
		{
			return this.UserProvider.Update(id, model) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpPatch("Namespace")]
		[HttpPatch("{id}/Namespace")]
		public async Task<IActionResult> SetNamespace(uint id = 0)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetNamespace(id, content) ? this.NoContent() : this.NotFound();
		}

		[HttpPatch("Name")]
		[HttpPatch("{id}/Name")]
		public async Task<IActionResult> SetName(uint id = 0)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetName(id, content) ? this.NoContent() : this.NotFound();
		}

		[HttpPatch("Nickname")]
		[HttpPatch("{id}/Nickname")]
		public async Task<IActionResult> SetNickname(uint id = 0)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetNickname(id, content) ? this.NoContent() : this.NotFound();
		}

		[HttpPatch("Email")]
		[HttpPatch("{id}/Email")]
		public async Task<IActionResult> SetEmail(uint id = 0)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetEmail(id, content, true) ? this.NoContent() : this.NotFound();
		}

		[HttpPatch("Phone")]
		[HttpPatch("{id}/Phone")]
		public async Task<IActionResult> SetPhone(uint id = 0)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetPhone(id, content, true) ? this.NoContent() : this.NotFound();
		}

		[HttpPatch("Description")]
		[HttpPatch("{id}/Description")]
		public async Task<IActionResult> SetDescription(uint id = 0)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetDescription(id, content) ? this.NoContent() : this.NotFound();
		}

		[HttpPatch("Status/{value}")]
		[HttpPatch("{id}/Status/{value}")]
		public Task<IActionResult> SetStatus(uint id, UserStatus value)
		{
			return this.UserProvider.SetStatus(id, value) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpHead("{id:required}")]
		[HttpGet("{id:required}/exists")]
		public Task<IActionResult> Exists(uint id)
		{
			return this.UserProvider.Exists(id) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[AllowAnonymous]
		[HttpHead("{namespace}:{identity}")]
		[HttpGet("exists/{namespace}:{identity}")]
		public Task<IActionResult> Exists(string @namespace, string identity)
		{
			if(string.IsNullOrWhiteSpace(identity))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.UserProvider.Exists(identity, @namespace) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[AllowAnonymous]
		[HttpPost("{id}/Verify/{type:required}")]
		public Task<IActionResult> Verify(uint id, string type, [FromQuery] string secret)
		{
			return this.UserProvider.Verify(id, type, secret) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.BadRequest());
		}
		#endregion

		#region 密码处理
		[HttpGet("Password")]
		[HttpGet("Password/Has")]
		public Task<IActionResult> HasPassword(uint id = 0)
		{
			return this.UserProvider.HasPassword(id) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[AllowAnonymous]
		[HttpGet("{identifier:required}/Password")]
		[HttpGet("{identifier:required}/Password/Has")]
		public Task<IActionResult> HasPassword(string identifier)
		{
			if(string.IsNullOrEmpty(identifier))
				return this.HasPassword(0U);
			if(uint.TryParse(identifier, out var id))
				return this.HasPassword(id);

			if(!IdentityQualifier.TryParse(identifier, out var identity))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.UserProvider.HasPassword(identity.Identity, identity.Namespace) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[Authorize]
		[HttpPut("{id}/Password/Change")]
		[HttpPut("Password/Change")]
		public Task<IActionResult> ChangePassword(uint id, [FromBody] PasswordChangeEntity password)
		{
			return this.UserProvider.ChangePassword(id, password.OldPassword, password.NewPassword) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[AllowAnonymous]
		[HttpPost("{identifier:required}/Password/Forget")]
		public Task<IActionResult> ForgetPassword(string identifier)
		{
			if(!IdentityQualifier.TryParse(identifier, out var identity))
				return Task.FromResult((IActionResult)this.BadRequest());

			var token = this.UserProvider.ForgetPassword(identity.Identity, identity.Namespace);

			return string.IsNullOrEmpty(token) ?
				Task.FromResult((IActionResult)this.NotFound()) :
				Task.FromResult((IActionResult)this.Content(token));
		}

		[AllowAnonymous]
		[HttpPost("Password/Reset/{token:required}")]
		public async Task<IActionResult> ResetPassword(string token, [FromQuery] string secret)
		{
			if(string.IsNullOrWhiteSpace(secret))
				return this.BadRequest();

			var password = await this.Request.ReadAsStringAsync();

			return this.UserProvider.ResetPassword(token, secret, password) ?
				this.NoContent() :
				this.NotFound();
		}

		[AllowAnonymous]
		[HttpPost("{identifier:required}/Password/Reset")]
		public Task<IActionResult> ResetPassword(string identifier, [FromBody] PasswordResetEntity content)
		{
			if(!IdentityQualifier.TryParse(identifier, out var identity))
				return Task.FromResult((IActionResult)this.BadRequest());

			if(content.Answers == null || content.Answers.Length < 3)
				Task.FromResult(this.BadRequest());

			return this.UserProvider.ResetPassword(identity.Identity, identity.Namespace, content.Answers, content.Password) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.BadRequest());
		}

		[HttpGet("Password/Questions")]
		public Task<IActionResult> GetPasswordQuestions(uint id = 0)
		{
			var result = this.UserProvider.GetPasswordQuestions(id);

			//如果返回的结果为空表示指定的表示的用户不存在
			if(result == null)
				return Task.FromResult((IActionResult)this.NotFound());

			//如果问题数组内容不是全空，则返回该数组
			for(int i = 0; i < result.Length; i++)
			{
				if(!string.IsNullOrEmpty(result[i]))
					return Task.FromResult((IActionResult)this.Ok(result));
			}

			//返回空消息
			return Task.FromResult((IActionResult)this.NoContent());
		}

		[AllowAnonymous]
		[HttpGet("{identifier:required}/Password/Questions")]
		public Task<IActionResult> GetPasswordQuestions(string identifier)
		{
			if(string.IsNullOrEmpty(identifier))
				return this.GetPasswordQuestions(0U);
			if(uint.TryParse(identifier, out var id))
				return this.GetPasswordQuestions(id);

			if(!IdentityQualifier.TryParse(identifier, out var identity))
				return Task.FromResult((IActionResult)this.BadRequest());

			var result = this.UserProvider.GetPasswordQuestions(identity.Identity, identity.Namespace);

			//如果返回的结果为空表示指定的表示的用户不存在
			if(result == null)
				return Task.FromResult((IActionResult)this.NotFound());

			//如果问题数组内容不是全空，则返回该数组
			for(int i = 0; i < result.Length; i++)
			{
				if(!string.IsNullOrEmpty(result[i]))
					return Task.FromResult((IActionResult)this.Ok(result));
			}

			//返回空消息
			return Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Password/Answers")]
		[HttpPut("Password/Answers")]
		public Task<IActionResult> SetPasswordQuestionsAndAnswers(uint id, [FromBody] PasswordQuestionsAndAnswersEntity content)
		{
			return this.UserProvider.SetPasswordQuestionsAndAnswers(id, content.Password, content.Questions, content.Answers) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion

		#region 成员操作
		[HttpGet("{id}/Ancestors")]
		public Task<IActionResult> GetAncestors(uint id)
		{
			var roles = this.MemberProvider.GetAncestors(id, MemberType.User);

			return roles != null ?
				Task.FromResult((IActionResult)this.Ok(roles)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpGet("{id}/Roles")]
		public Task<IActionResult> GetRoles(uint id = 0)
		{
			if(id == 0)
				id = this.User.Identity.GetIdentifier<uint>();

			var roles = this.MemberProvider.GetRoles(id, MemberType.User);

			return roles != null ?
				Task.FromResult((IActionResult)this.Ok(roles)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Roles")]
		public async Task<IActionResult> SetRoles(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			var members = Zongsoft.Common.StringExtension.Slice<uint>(content, ',', uint.TryParse).Select(roleId => new Member(roleId, id, MemberType.User));
			return this.MemberProvider.SetMembers(members) > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetRoles), new { id }, members) : this.NotFound();
		}

		[HttpGet("In/{roles:required}")]
		[HttpGet("{id}/In/{roles:required}")]
		public Task<IActionResult> InRole(uint id, string roles)
		{
			if(string.IsNullOrWhiteSpace(roles))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.Authorizer.InRoles(id, Zongsoft.Common.StringExtension.Slice(roles, ',', '|').ToArray()) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion

		#region 授权操作
		[HttpGet("{id}/Authorize/{target}:{actionId}")]
		[HttpGet("Authorize/{target}:{actionId}")]
		public Task<IActionResult> Authorize(uint id, string target, string actionId)
		{
			if(string.IsNullOrWhiteSpace(target))
				return Task.FromResult((IActionResult)this.BadRequest("Missing schema for the authorize operation."));
			if(string.IsNullOrWhiteSpace(actionId))
				return Task.FromResult((IActionResult)this.BadRequest("Missing action for the authorize operation."));

			return this.Authorizer.Authorize(id, target, actionId) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("{id}/Authorizes")]
		[HttpGet("Authorizes/{id?}")]
		public IActionResult Authorizes(uint id = 0)
		{
			if(id == 0)
				id = this.User.Identity.GetIdentifier<uint>();

			return this.Ok(this.Authorizer.Authorizes(id, MemberType.User).Select(p =>
				p.Target + ":" + string.Join(',', p.Actions.Select(a => a.Name).ToArray())
			));
		}
		#endregion

		#region 权限操作
		[HttpGet("{id}/Permissions/{target?}", Name = nameof(GetPermissions))]
		[HttpGet("Permissions/{target?}")]
		public Task<IActionResult> GetPermissions(uint id, string target = null)
		{
			var result = this.PermissionProvider.GetPermissions(id, MemberType.User, target)
				.GroupBy(p => p.Target)
				.ToDictionary(group => group.Key, elements => elements.Select(element => element.Action + ":" + element.Granted.ToString()));

			return (result != null && result.Count > 0) ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Permissions")]
		[HttpPut("Permissions")]
		public Task<IActionResult> SetPermissions(uint id, [FromBody] IEnumerable<PermissionModel> permissions, [FromQuery] bool reset = false)
		{
			return this.PermissionProvider.SetPermissions(id, MemberType.User, permissions, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtRoute(nameof(GetPermissions), new { id }, null)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}/Permission/{target}:{actionId}")]
		[HttpDelete("Permission/{target}:{actionId}")]
		public Task<IActionResult> RemovePermission(uint id, string target, string actionId = null)
		{
			if(string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(actionId))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.PermissionProvider.RemovePermissions(id, MemberType.User, target, actionId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id}/Permissions/{target?}")]
		[HttpDelete("Permissions/{target?}")]
		public Task<IActionResult> RemovePermissions(uint id, string target = null)
		{
			return this.PermissionProvider.RemovePermissions(id, MemberType.User, target) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("{id}/Permission.Filters/{target?}", Name = nameof(GetPermissionFilters))]
		[HttpGet("Permission.Filters/{target?}")]
		public Task<IActionResult> GetPermissionFilters(uint id, string target = null)
		{
			var result = this.PermissionProvider.GetPermissionFilters(id, MemberType.User, target)
				.GroupBy(p => p.Target)
				.ToDictionary(group => group.Key, elements => elements.Select(element => element.Action + ":" + element.Filter));

			return (result != null && result.Count > 0) ?
				Task.FromResult((IActionResult)this.Ok(result)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpPut("{id}/Permission.Filters")]
		[HttpPut("Permission.Filters")]
		public Task<IActionResult> SetPermissionFilters(uint id, [FromBody] IEnumerable<PermissionFilterModel> permissions, [FromQuery] bool reset = false)
		{
			return this.PermissionProvider.SetPermissionFilters(id, MemberType.User, permissions, reset) > 0 ?
				Task.FromResult((IActionResult)this.CreatedAtRoute(nameof(GetPermissionFilters), new { id }, null)) :
				Task.FromResult((IActionResult)this.NoContent());
		}

		[HttpDelete("{id}/Permission.Filter/{target}:{actionId}")]
		[HttpDelete("Permission.Filter/{target}:{actionId}")]
		public Task<IActionResult> RemovePermissionFilter(uint id, string target, string actionId)
		{
			if(string.IsNullOrEmpty(target) || string.IsNullOrEmpty(actionId))
				return Task.FromResult((IActionResult)this.BadRequest());

			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.User, target, actionId) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpDelete("{id}/Permission.Filters/{target?}")]
		[HttpDelete("Permission.Filters/{target?}")]
		public Task<IActionResult> RemovePermissionFilters(uint id, string target = null)
		{
			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.User, target) > 0 ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}
		#endregion

		#region 内部结构
		public struct RegisterEntity
		{
			public string FullName { get; set; }
			public string Identity { get; set; }
			public string Password { get; set; }
			public string Namespace { get; set; }
			public string Description { get; set; }
			public IDictionary<string, object> Parameters { get; set; }
		}

		public struct PasswordChangeEntity
		{
			public string OldPassword { get; set; }
			public string NewPassword { get; set; }
		}

		public struct PasswordResetEntity
		{
			public string Password { get; set; }
			public string[] Answers { get; set; }
		}

		public struct PasswordQuestionsAndAnswersEntity
		{
			public string Password { get; set; }
			public string[] Questions { get; set; }
			public string[] Answers { get; set; }
		}
		#endregion
	}
}
