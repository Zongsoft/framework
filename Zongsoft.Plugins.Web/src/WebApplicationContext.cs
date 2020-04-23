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
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Claims;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Web
{
	public class WebApplicationContext : PluginApplicationContext
	{
		#region 成员字段
		private IHttpContextAccessor _http;
		#endregion

		#region 构造函数
		public WebApplicationContext(IServiceProvider services) : base(services)
		{
			_http = this.Services.GetRequiredService<IHttpContextAccessor>();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前Web应用程序的上下文对象。
		/// </summary>
		public HttpContext HttpContext
		{
			get => _http?.HttpContext;
		}
		#endregion

		#region 重写方法
		public override ClaimsPrincipal Principal
		{
			get => _http?.HttpContext.User;
		}

		protected override IWorkbenchBase CreateWorkbench(out PluginTreeNode node)
		{
			return base.CreateWorkbench(out node) ?? new Workbench(this);
		}
		#endregion
	}
}
