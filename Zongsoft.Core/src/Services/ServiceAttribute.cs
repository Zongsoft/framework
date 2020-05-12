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

namespace Zongsoft.Services
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class ServiceAttribute : Attribute
	{
		#region 构造函数
		public ServiceAttribute(params Type[] contracts)
		{
			this.Contracts = contracts;
		}
		#endregion

		#region 公共属性
        /// <summary>
        /// 获取服务的契约类型数组，如果为空则表示服务的类型即为该注解所标示的类型。
        /// </summary>
		public Type[] Contracts { get; }

        /// <summary>
        /// 获取或设置该注解所标示的静态类的成员名(属性或字段)，多个成员名之间采用逗号分隔。
        /// </summary>
		public string Members { get; set; }
		#endregion
	}
}
