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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

namespace Zongsoft.Web
{
	public class ControllerFeatureProvider : Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider
	{
		#region 静态属性
		/// <summary>获取忽略的控制器类型集。</summary>
		public static ICollection<Type> Ignores { get; } = new HashSet<Type>();
		#endregion

		#region 重写风法
		protected override bool IsController(TypeInfo typeInfo) => IsControllerType(typeInfo);
		#endregion

		#region 静态方法
		internal static bool IsControllerType(Type type)
		{
			const string CONTROLLER_SUFFIX = "Controller";

			if(!type.IsClass)
				return false;

			if(type.IsAbstract)
				return false;

			/*
			 * 注：支持控制器为公共嵌套类，参考 Type.IsPublic 属性值的问题：
			 * https://learn.microsoft.com/zh-cn/dotnet/api/system.type.ispublic
			 */

			if(!type.IsPublic && (!type.IsNested || type.IsNestedPrivate))
				return false;

			if(type.ContainsGenericParameters)
				return false;

			if(type.IsDefined(typeof(NonControllerAttribute)))
				return false;

			if(!type.Name.EndsWith(CONTROLLER_SUFFIX, StringComparison.OrdinalIgnoreCase) &&
			   !type.IsDefined(typeof(ControllerAttribute)) &&
			   !type.IsDefined(typeof(ControllerNameAttribute)))
				return false;

			if(Ignores.Contains(type))
				return false;

			return true;
		}
		#endregion
	}
}