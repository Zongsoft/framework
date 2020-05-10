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
using System.Reflection;
using System.Security.Claims;
using System.Collections.Generic;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Zongsoft.Web
{
	public class WebApplicationContext : Zongsoft.Plugins.PluginApplicationContext
	{
		#region 成员字段
		private IHttpContextAccessor _http;
		#endregion

		#region 构造函数
		public WebApplicationContext(IServiceProvider services) : base(services)
		{
		}
		#endregion

		#region 公共属性
		/// <inheritdoc />
		public override ClaimsPrincipal Principal
		{
			get => this.HttpContext.User;
		}

		/// <summary>
		/// 获取当前Web应用程序的上下文对象。
		/// </summary>
		public HttpContext HttpContext
		{
			get
			{
				if(_http == null)
					_http = this.Services.GetRequiredService<IHttpContextAccessor>();

				return _http.HttpContext;
			}
		}
		#endregion

		#region 重写方法
		public override void Initialize()
		{
			base.Initialize();

			//加载插件中的Web程序集部件
			PopulateApplicationParts(this.Services.GetRequiredService<ApplicationPartManager>().ApplicationParts, this.Plugins);
		}

		protected override Plugins.IWorkbenchBase CreateWorkbench(out Plugins.PluginTreeNode node)
		{
			return base.CreateWorkbench(out node) ?? new Workbench(this);
		}
		#endregion

		#region 私有方法
		private static void PopulateApplicationParts(ICollection<ApplicationPart> parts, IEnumerable<Plugins.Plugin> plugins)
		{
			if(parts == null || plugins == null)
				return;

			foreach(var plugin in plugins)
			{
				var assemblies = plugin.Manifest.Assemblies;

				for(int i = 0; i < assemblies.Length; i++)
				{
					if(IsWebAssembly(assemblies[i]))
						parts.Add(new AssemblyPart(assemblies[i]));
				}

				if(plugin.HasChildren)
					PopulateApplicationParts(parts, plugin.Children);
			}
		}

		private static bool IsWebAssembly(Assembly assembly)
		{
			var references = assembly.GetReferencedAssemblies();

			for(int i = 0; i < references.Length; i++)
			{
				if(references[i].Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal))
					return true;
			}

			return false;
		}
		#endregion
	}
}
