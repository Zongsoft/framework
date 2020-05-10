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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[Area(Modules.Security)]
	[Authorize]
	[Authorization]
	public class UserController : ControllerBase
	{
		#region 成员字段
		private IUserProvider _userProvider;
		private IAuthorizer _authorizer;
		private IMemberProvider _memberProvider;
		private IPermissionProvider _permissionProvider;
		#endregion

		#region 公共属性
		[ServiceDependency(IsRequired = true)]
		public IAuthorizer Authorizer
		{
			get => _authorizer;
			set => _authorizer = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IUserProvider UserProvider
		{
			get => _userProvider;
			set => _userProvider = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency(IsRequired = true)]
		public IMemberProvider MemberProvider
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
		/// <summary>
		/// 查询指定编号或用户标识、命名空间的用户。
		/// </summary>
		/// <param name="id">指定的路由参数，如果该参数为纯数字则会被当做为用户编号；否则请参考备注部分的处理规则。</param>
		/// <param name="paging">指定的查询分页设置。</param>
		/// <returns>返回的用户或用户集。</returns>
		/// <remarks>
		///		<para>注意：由于路由匹配约定，对于首字符为字母并且中间字符为字母、数字、下划线的路由数据并不会被匹配为<paramref name="id"/>，
		///		因此对于查询用户标识和命名空间的组合条件，该参数应该使用冒号进行组合；而对于查询指定命名空间内的所有用户则应以冒号结尾，大致示意如下：</para>
		///		<list type="bullet">
		///			<listheader>
		///				<term>URL</term>
		///				<description>备注</description>
		///			</listheader>
		///			<item>
		///				<term>/api/Security/User/*</term>
		///				<description>查询系统中的所有用户，即忽略命名空间。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User</term>
		///				<description>查询命名空间为空的所有用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/101</term>
		///				<description>查询用户<seealso cref="User.UserId"/>为101的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/administrator</term>
		///				<description>查询用户<seealso cref="User.Name"/>为：Administrator，且<seealso cref="User.Namespace"/>为空的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/13812345678!phone</term>
		///				<description>
		///					<para>因为手机号与用户编号都是数字，所以必须以叹号分隔的后缀以示区别。</para>
		///					<para>查询用户<seealso cref="User.Phone"/>为：13812345678，且<seealso cref="User.Namespace"/>为空的用户。</para>
		///				</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/popeye@zongsoft.com</term>
		///				<description>查询用户<seealso cref="User.Email"/>为：popeye@zongsoft.com，且<seealso cref="User.Namespace"/>为空的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/zongsoft:popeye</term>
		///				<description>查询用户<seealso cref="User.Name"/>为：Popeye，且<seealso cref="User.Namespace"/>为：zongsoft的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/zongsoft:13812345678</term>
		///				<description>查询用户<seealso cref="User.Phone"/>为：13812345678，且<seealso cref="User.Namespace"/>为：zongsoft的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/zongsoft:popeye@zongsoft.com</term>
		///				<description>查询用户<seealso cref="User.Email"/>为：popeye@zongsoft.com，且<seealso cref="User.Namespace"/>为：zongsoft的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/zongsoft:*</term>
		///				<description>查询<seealso cref="User.Namespace"/>为：zongsoft的所有用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/:admin</term>
		///				<description>查询<seealso cref="User.Namespace"/>为空，且用户<seealso cref="User.Name"/>为：Admin 的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/:13812345678</term>
		///				<description>查询<seealso cref="User.Namespace"/>为空，且用户<seealso cref="User.Phone"/>为：13812345678 的用户。</description>
		///			</item>
		///			<item>
		///				<term>/api/Security/User/:zongsoft@gmail.com</term>
		///				<description>查询<seealso cref="User.Namespace"/>为空，且用户<seealso cref="User.Email"/>为：zongsoft@gmail.com 的用户。</description>
		///			</item>
		///		</list>
		/// </remarks>
		public virtual IActionResult Get(string id = null, [FromQuery]Paging paging = null)
		{
			IUser user;

			//如果标识为空或星号，则进行多用户查询
			if(string.IsNullOrEmpty(id) || id == "*")
				return this.Ok(this.UserProvider.GetUsers(id, paging));

			//解析用户标识参数
			var userId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);

			//如果ID参数是数字则以编号方式返回唯一的用户信息
			if(userId > 0)
			{
				user = this.UserProvider.GetUser(userId);
				return user == null ? (IActionResult)this.NotFound() : this.Ok();
			}

			//如果用户标识为空或星号，则进行命名空间查询
			if(string.IsNullOrEmpty(identity) || identity == "*")
				return this.Ok(this.UserProvider.GetUsers(@namespace, paging));

			//返回指定标识的用户信息
			user = this.UserProvider.GetUser(identity, @namespace);
			return user == null ? (IActionResult)this.NotFound() : this.Ok(user);
		}

		public virtual IActionResult Delete(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var count = this.UserProvider.Delete(Common.StringExtension.Slice<uint>(id, chr => chr == ',' || chr == '|', uint.TryParse).Where(p => p > 0).ToArray());
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}

		public virtual IActionResult Post(IUser model)
		{
			if(model == null)
				return this.BadRequest();

			//从请求消息的头部获取指定的用户密码
			this.Request.Headers.TryGetValue("x-password", out var password);

			if(this.UserProvider.Create(model, password))
				return this.CreatedAtAction(nameof(Get), model.UserId);

			return this.Conflict();
		}

		[HttpPatch]
		[ActionName("Namespace")]
		public async Task<IActionResult> SetNamespace(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetNamespace(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Name")]
		public async Task<IActionResult> SetName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetName(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("FullName")]
		public async Task<IActionResult> SetFullName(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetFullName(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Email")]
		public async Task<IActionResult> SetEmail(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetEmail(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Phone")]
		public async Task<IActionResult> SetPhone(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetPhone(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Description")]
		public async Task<IActionResult> SetDescription(uint id)
		{
			var content = await this.Request.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return this.BadRequest();

			return this.UserProvider.SetDescription(id, content) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPatch]
		[ActionName("Status")]
		public virtual IActionResult SetStatus(uint id, [FromRoute()]UserStatus status)
		{
			return this.UserProvider.SetStatus(id, status) ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpGet]
		[AllowAnonymous]
		[Authorization(Suppressed = true)]
		public virtual IActionResult Exists(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var userId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);
			var existed = userId > 0 ?
				this.UserProvider.Exists(userId) :
				this.UserProvider.Exists(identity, @namespace);

			return existed ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpGet]
		[AllowAnonymous]
		[Authorization(Suppressed = true)]
		public IActionResult Verify(uint id, [FromRoute()]string type, [FromQuery]string secret)
		{
			return this.UserProvider.Verify(id, type, secret) ?
			       (IActionResult)this.Ok() : this.BadRequest();
		}
		#endregion

		#region 密码处理
		[HttpGet]
		public IActionResult HasPassword(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			bool existed;
			var userId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);

			if(userId > 0)
				existed = this.UserProvider.HasPassword(userId);
			else
				existed = this.UserProvider.HasPassword(identity, @namespace);

			return existed ? (IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPut]
		public IActionResult ChangePassword(uint id, PasswordChangeEntity password)
		{
			return this.UserProvider.ChangePassword(id, password.OldPassword, password.NewPassword) ?
				(IActionResult)this.Ok() : this.NotFound();
		}

		[HttpPost]
		[AllowAnonymous]
		[Authorization(Suppressed = true)]
		public IActionResult ForgetPassword(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var parts = id.Split(':');
			var userId = parts.Length > 1 ?
				this.UserProvider.ForgetPassword(parts[1], parts[0]) :
				this.UserProvider.ForgetPassword(parts[0], null);

			return userId == 0 ? (IActionResult)this.NotFound() : this.Ok(userId);
		}

		[HttpPost]
		[AllowAnonymous]
		[Authorization(Suppressed = true)]
		public IActionResult ResetPassword(string id, [FromBody]PasswordResetEntity content)
		{
			if(!string.IsNullOrWhiteSpace(content.Secret))
			{
				if(!uint.TryParse(id, out var userId))
					return this.BadRequest("Invalid id argument, it must be a integer.");

				if(!this.UserProvider.ResetPassword(userId, content.Secret, content.Password))
					return this.NotFound();
			}
			else if(content.PasswordAnswers != null && content.PasswordAnswers.Length > 0)
			{
				var userId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);

				//注意：该方法会将传入的纯数字的标识当做手机号处理
				if(userId > 0)
					identity = userId.ToString();

				if(!this.UserProvider.ResetPassword(identity, @namespace, content.PasswordAnswers, content.Password))
					return this.NotFound();
			}
			else
			{
				return this.BadRequest();
			}

			return this.Ok();
		}

		[HttpGet]
		[AllowAnonymous]
		[ActionName("PasswordQuestions")]
		[Authorization(Suppressed = true)]
		public IActionResult GetPasswordQuestions(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var userId = Utility.ResolvePattern(id, out var identity, out var @namespace, out _);
			string[] result;

			if(userId > 0)
				result = this.UserProvider.GetPasswordQuestions(userId);
			else
				result = this.UserProvider.GetPasswordQuestions(identity, @namespace);

			//如果返回的结果为空表示指定的表示的用户不存在
			if(result == null)
				return this.NotFound();

			//如果问题数组内容不是全空，则返回该数组
			for(int i = 0; i < result.Length; i++)
			{
				if(!string.IsNullOrEmpty(result[i]))
					return this.Ok(result);
			}

			//返回空消息
			return this.NoContent();
		}

		[HttpPut]
		[ActionName("PasswordAnswers")]
		public IActionResult SetPasswordQuestionsAndAnswers(uint id, [FromBody]PasswordQuestionsAndAnswersEntity content)
		{
			return this.UserProvider.SetPasswordQuestionsAndAnswers(id, content.Password, content.Questions, content.Answers) ?
				(IActionResult)this.Ok() : this.NotFound();
		}
		#endregion

		#region 成员方法
		[HttpGet]
		[ActionName("Roles")]
		public IEnumerable<IRole> GetRoles(uint id)
		{
			return this.MemberProvider.GetRoles(id, MemberType.User);
		}

		[HttpGet]
		[ActionName("In")]
		public IActionResult InRole(uint id, [FromRoute()]string roles)
		{
			if(string.IsNullOrEmpty(roles))
				return this.BadRequest();

			return this.Authorizer.InRoles(id, Common.StringExtension.Slice(roles, ',', ';', '|').ToArray()) ?
			       (IActionResult)this.Ok() :
			       (IActionResult)this.NotFound();
		}
		#endregion

		#region 授权方法
		[HttpGet]
		[Route("{id:int}/{schemaId}:{actionId}")]
		public IActionResult Authorize(uint id, [FromRoute()]string schemaId, [FromRoute()]string actionId)
		{
			if(string.IsNullOrWhiteSpace(schemaId))
				return this.BadRequest("Missing schema for the authorize operation.");

			if(string.IsNullOrWhiteSpace(actionId))
				return this.BadRequest("Missing action for the authorize operation.");

			return this.Authorizer.Authorize(id, schemaId, actionId) ?
				(IActionResult)this.Ok() : this.Forbid();
		}

		[HttpGet]
		public IEnumerable<AuthorizationToken> Authorizes(uint id)
		{
			return this.Authorizer.Authorizes(id, MemberType.User);
		}
		#endregion

		#region 权限方法
		[HttpGet]
		[ActionName("Permissions")]
		public IEnumerable<Permission> GetPermissions(uint id, [FromRoute()]string schemaId = null)
		{
			return this.PermissionProvider.GetPermissions(id, MemberType.User, schemaId);
		}

		[HttpPost]
		[ActionName("Permissions")]
		public IActionResult SetPermissions(uint id, [FromRoute()]string schemaId, [FromBody]IEnumerable<Permission> permissions, [FromQuery]bool reset = false)
		{
			var count = this.PermissionProvider.SetPermissions(id, MemberType.User, schemaId, permissions, reset);
			return count > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetPermissions), id) : this.NoContent();
		}

		[HttpDelete]
		[ActionName("Permission")]
		public IActionResult RemovePermission(uint id, [FromRoute()]string schemaId, [FromRoute()]string actionId)
		{
			if(string.IsNullOrEmpty(schemaId) || string.IsNullOrEmpty(actionId))
				return this.BadRequest();

			return this.PermissionProvider.RemovePermissions(id, MemberType.User, schemaId, actionId) > 0 ?
				(IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpDelete]
		[ActionName("Permissions")]
		public IActionResult RemovePermissions(uint id, [FromRoute()]string schemaId = null)
		{
			var count = this.PermissionProvider.RemovePermissions(id, MemberType.User, schemaId);
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}

		[HttpGet]
		[ActionName("PermissionFilters")]
		public IEnumerable<PermissionFilter> GetPermissionFilters(uint id, [FromRoute()]string schemaId = null)
		{
			return this.PermissionProvider.GetPermissionFilters(id, MemberType.User, schemaId);
		}

		[HttpPost]
		[ActionName("PermissionFilters")]
		public IActionResult SetPermissionFilters(uint id, [FromRoute()]string schemaId, [FromBody]IEnumerable<PermissionFilter> permissions, [FromQuery]bool reset = false)
		{
			var count = this.PermissionProvider.SetPermissionFilters(id, MemberType.User, schemaId, permissions, reset);
			return count > 0 ? (IActionResult)this.CreatedAtAction(nameof(GetPermissionFilters), id) : this.NoContent();
		}

		[HttpDelete]
		[ActionName("PermissionFilter")]
		public IActionResult RemovePermissionFilter(uint id, [FromRoute()]string schemaId, [FromRoute()]string actionId)
		{
			if(string.IsNullOrEmpty(schemaId) || string.IsNullOrEmpty(actionId))
				return this.BadRequest();

			return this.PermissionProvider.RemovePermissionFilters(id, MemberType.User, schemaId, actionId) > 0 ?
				(IActionResult)this.NoContent() : this.NotFound();
		}

		[HttpDelete]
		[ActionName("PermissionFilters")]
		public IActionResult RemovePermissionFilters(uint id, [FromRoute()]string schemaId = null)
		{
			var count = this.PermissionProvider.RemovePermissionFilters(id, MemberType.User, schemaId);
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}
		#endregion

		#region 内部结构
		public struct PasswordChangeEntity
		{
			public string OldPassword;
			public string NewPassword;
		}

		public struct PasswordResetEntity
		{
			public string Secret;
			public string Password;
			public string[] PasswordAnswers;
		}

		public struct PasswordQuestionsAndAnswersEntity
		{
			public string Password;
			public string[] Questions;
			public string[] Answers;
		}
		#endregion
	}
}
