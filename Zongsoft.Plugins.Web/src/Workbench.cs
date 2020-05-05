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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Web
{
	public class Workbench : Zongsoft.Plugins.WorkbenchBase
	{
		#region 构造函数
		internal Workbench(WebApplicationContext applicationContext) : base(applicationContext)
		{
		}
		#endregion

		#region 重写方法
		protected override void OnOpen()
		{
			//调用基类同名方法，以启动工作台下Startup下的所有工作者
			base.OnOpen();

			//加载插件中的Web程序集部件
			this.LoadApplicationParts();
		}
		#endregion

		private void LoadApplicationParts()
		{
			var manager = this.ApplicationContext.Services.GetService<ApplicationPartManager>();

			if(manager == null)
				return;

			foreach(var plugin in this.ApplicationContext.PluginTree.Plugins)
			{
				var assemblies = plugin.Manifest.Assemblies;

				if(assemblies == null || assemblies.Length == 0)
					continue;

				for(int i = 0; i < assemblies.Length; i++)
				{
					if(IsWebAssembly(assemblies[i]))
						manager.ApplicationParts.Add(new AssemblyPart(assemblies[i]));
				}
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
	}
}
