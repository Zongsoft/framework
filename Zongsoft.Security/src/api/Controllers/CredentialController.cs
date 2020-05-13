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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[Area(Modules.Security)]
	[Authorize]
	[Authorization]
	[Obsolete]
	public class CredentialController : ControllerBase
	{
		#region 成员字段
		private ICredentialProvider _credentialProvider;
		#endregion

		#region 公共属性
		[ServiceDependency]
		public ICredentialProvider CredentialProvider
		{
			get => _credentialProvider;
			set => _credentialProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		public IActionResult Get(string id)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			Credential credential;
			var index = id.LastIndexOfAny(new[] { '!', '@' });

			if(index > 0 && index < id.Length - 1)
				credential = this.CredentialProvider.GetCredential(id.Substring(0, index), id.Substring(index + 1));
			else
				credential = this.CredentialProvider.GetCredential(id);

			if(credential == null)
				return this.NoContent();
			else
				return this.Ok(credential);
		}

		public void Delete(string id)
		{
			if(id != null && id.Length > 0)
				this.CredentialProvider.Unregister(id);
		}

		[HttpGet]
		public IActionResult Renew(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var credential = this.CredentialProvider.Renew(id);
			return credential == null ? (IActionResult)this.BadRequest() : this.Ok(credential);
		}
		#endregion
	}
}
