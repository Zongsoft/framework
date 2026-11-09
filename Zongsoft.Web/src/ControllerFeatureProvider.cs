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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

namespace Zongsoft.Web;

public class ControllerFeatureProvider : Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider
{
	#region 静态属性
	/// <summary>获取忽略的控制器类型集。</summary>
	public static ICollection<Type> Ignores { get; } = new HashSet<Type>();
	#endregion

	#region 重写方法
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