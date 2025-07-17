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

namespace Zongsoft.Services;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ServiceAttribute : Attribute
{
	#region 构造函数
	public ServiceAttribute(params Type[] contracts) : this(null, contracts) { }
	public ServiceAttribute(string name, params Type[] contracts)
	{
		this.Name = name;
		this.Contracts = contracts;
	}
	#endregion

	#region 公共属性
	/// <summary>获取服务的名称。</summary>
	public string Name { get; }

	/// <summary>获取服务的契约类型数组，如果为空(<c>null</c>)或空集则表示服务的类型即为该注解所标示的类。</summary>
	public Type[] Contracts { get; }

	/// <summary>获取或设置服务的标签，多个标签之间使用逗号(<c>,</c>)或分号(<c>;</c>)分隔。</summary>
	public string Tags { get; set; }

	/// <summary>获取或设置该注解所标示的静态类的成员名(属性或字段)，多个成员名之间使用逗号(<c>,</c>)分隔。</summary>
	public string Members { get; set; }
	#endregion

	#region 公共方法
	public bool TryGetTags(out string[] tags)
	{
		if(string.IsNullOrEmpty(this.Tags))
		{
			tags = null;
			return false;
		}

		tags = this.Tags.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		return tags.Length > 0;
	}
	#endregion
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ServiceAttribute<TContract>(string name = null) : ServiceAttribute(name, typeof(TContract))
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ServiceAttribute<TContract1, TContract2>(string name = null) : ServiceAttribute(name, typeof(TContract1), typeof(TContract2))
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ServiceAttribute<TContract1, TContract2, TContract3>(string name = null) : ServiceAttribute(name, typeof(TContract1), typeof(TContract2), typeof(TContract3))
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ServiceAttribute<TContract1, TContract2, TContract3, TContract4>(string name = null) : ServiceAttribute(name, typeof(TContract1), typeof(TContract2), typeof(TContract3), typeof(TContract4))
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class ServiceAttribute<TContract1, TContract2, TContract3, TContract4, TContract5>(string name = null) : ServiceAttribute(name, typeof(TContract1), typeof(TContract2), typeof(TContract3), typeof(TContract4), typeof(TContract5))
{
}
