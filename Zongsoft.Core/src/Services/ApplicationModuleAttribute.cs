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
using System.Reflection;

namespace Zongsoft.Services;

[AttributeUsage(AttributeTargets.Assembly)]
public class ApplicationModuleAttribute : Attribute
{
	#region 构造函数
	public ApplicationModuleAttribute(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取应用模块的名称。</summary>
	public string Name { get; }
	#endregion

	#region 静态方法
	public static ApplicationModuleAttribute Find(Type type) => Find(type?.Assembly);
	public static ApplicationModuleAttribute Find(Assembly assembly)
	{
		if(assembly == null || assembly.IsDynamic)
			return null;

		var attribute = assembly.GetCustomAttribute<ApplicationModuleAttribute>();
		if(attribute != null)
			return attribute;

		/*
		 * 关于模块化依赖的约定：
		 * 如果指定类型所在的程序集没有定义应用模块注解，则尝试从其引用的程序集中找到它的“主程序集”作为应用模块的定义者。
		 * 
		 * 示例：
		 *	- Automao.Things.Web
		 *	- Automao.Things.Services
		 *	- Automao.Things.Protocols.Mqtt
		 *	对于上面程序集而言，约定其“主程序集”名称为：`Automao.Things`。
		 *	注意：假设没有 `Automao.Things.Protocols` 程序集或该程序集没有定义应用模块注解。
		 */

		var name = assembly.GetName().Name;
		var index = name.LastIndexOf('.');

		while(index > 0)
		{
			var findable = name[..index];
			var assemblies = assembly.GetReferencedAssemblies();

			for(int i = 0; i < assemblies.Length; i++)
			{
				if(assemblies[i].Name != findable)
					continue;

				var found = Match(assemblies[i]);
				if(found != null)
					return found.GetCustomAttribute<ApplicationModuleAttribute>();
			}

			//继续向前查找
			index = name.LastIndexOf('.', index - 1);
		}

		return null;

		static Assembly Match(AssemblyName name)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			for(int i = 0; i < assemblies.Length; ++i)
			{
				if(assemblies[i].GetName().Name == name.Name)
					return assemblies[i];
			}

			return null;
		}
	}
	#endregion
}
