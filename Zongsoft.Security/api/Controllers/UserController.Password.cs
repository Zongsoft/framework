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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using Zongsoft.Web.Http;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

partial class UserController
{
	[ControllerName("Password")]
	public class PasswordController : ControllerBase
	{
		#region 公共属性
		public IUserService Service => Authentication.Servicer.Users;
		#endregion

		#region 公共方法
		[AllowAnonymous]
		[HttpGet("[area]/{id:required}/[controller]")]
		[HttpGet("[area]/{id:required}/[controller]/[action]")]
		public async Task<IActionResult> HasAsync(string id, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			return await this.Service.HasPasswordAsync(new Identifier(typeof(IUser), id), cancellation) ? this.Content("Yes!") : this.NoContent();
		}

		[HttpPut("[area]/[controller]/[action]")]
		[HttpPut("[area]/{id}/[controller]/[action]")]
		public async Task<IActionResult> ChangeAsync(string id, [FromBody]PasswordChangeModel password, CancellationToken cancellation = default)
		{
			return await this.Service.ChangePasswordAsync(new Identifier(typeof(IUser), id), password.OldPassword, password.NewPassword, cancellation) ?
				this.NoContent() : this.NotFound();
		}

		[AllowAnonymous]
		[HttpPost("[area]/{identifier:required}/[controller]/[action]")]
		public async Task<IActionResult> ForgetAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return this.BadRequest();

			(var identity, var @namespace) = Utility.Identify(identifier);
			if(string.IsNullOrEmpty(identity))
				return this.BadRequest();

			var token = await this.Service.ForgetPasswordAsync(identity, @namespace, new Parameters(this.Request.GetParameters()), cancellation);
			return string.IsNullOrEmpty(token) ? this.NotFound() : this.Content(token);
		}

		[AllowAnonymous]
		[HttpPost("[area]/[controller]/[action]/{token:required}")]
		public async Task<IActionResult> ResetAsync(string token, [FromQuery]string secret, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(token) || string.IsNullOrEmpty(secret))
				return this.BadRequest();

			var password = await this.Request.ReadAsStringAsync();
			return await this.Service.ResetPasswordAsync(token, secret, password, cancellation) ? this.NoContent() : this.NotFound();
		}

		[AllowAnonymous]
		[HttpPost("[area]/{identifier:required}/[controller]/[action]")]
		public async Task<IActionResult> ResetAsync(string identifier, [FromBody]PasswordResetModel content, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return this.BadRequest();

			(var identity, var @namespace) = Utility.Identify(identifier);
			if(string.IsNullOrEmpty(identity))
				return this.BadRequest();

			if(content.Answers == null || content.Answers.Length < 3 || content.Answers.Length > 5)
				return this.BadRequest();

			return await this.Service.ResetPasswordAsync(identity, @namespace, content.Answers, content.Password, cancellation) ? this.NoContent() : this.NotFound();
		}

		[AllowAnonymous]
		[HttpGet("[area]/{identifier:required}/[controller]/[action]")]
		public async Task<IActionResult> GetQuestionsAsync(string identifier, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(identifier))
				return this.BadRequest();

			(var identity, var @namespace) = Utility.Identify(identifier);
			if(string.IsNullOrEmpty(identity))
				return this.BadRequest();

			var result = await this.Service.GetPasswordQuestionsAsync(identity, @namespace, cancellation);

			//如果返回的结果为空表示指定的表示的用户不存在
			if(result == null)
				return this.NotFound();

			return result.Length == 0 ? this.NoContent() : this.Content(string.Join('\n', result));
		}

		[ActionName("Answers")]
		[HttpPut("[area]/{id}/[controller]/[action]")]
		[HttpPut("[area]/{id}/[controller]/Questions+Answers")]
		public async Task<IActionResult> SetQuestionsAndAnswers(string id, [FromBody]QuestionsAndAnswersModel content, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			if(content.HasQuestions && content.HasAnswers && content.Questions.Length != content.Answers.Length)
				return this.BadRequest();

			return await this.Service.SetPasswordQuestionsAndAnswersAsync(
				new Identifier(typeof(IUser), id),
				content.Password,
				content.Questions,
				content.Answers,
				cancellation) ? this.NoContent() : this.NotFound();
		}
		#endregion

		#region 嵌套结构
		public struct PasswordChangeModel
		{
			public string OldPassword { get; set; }
			public string NewPassword { get; set; }
		}

		public struct PasswordResetModel
		{
			public string Password { get; set; }
			public string[] Answers { get; set; }
		}

		public struct QuestionsAndAnswersModel
		{
			public string Password { get; set; }
			public string[] Questions { get; set; }
			public string[] Answers { get; set; }

			public readonly bool HasQuestions => this.Questions != null && this.Questions.Length > 0;
			public readonly bool HasAnswers => this.Answers != null && this.Answers.Length > 0;
		}
		#endregion
	}
}
