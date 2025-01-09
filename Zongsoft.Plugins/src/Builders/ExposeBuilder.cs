﻿/*
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

namespace Zongsoft.Plugins.Builders
{
	/// <summary>
	/// 构件暴露创建器。
	/// </summary>
	/// <remarks>
	/// 	<para>该构建器区别于<seealso cref="ObjectBuilder"/>的主要特征在于它不会执行追加操作。</para>
	/// </remarks>
	public class ExposeBuilder : ObjectBuilder
	{
		public override object Build(BuilderContext context)
		{
			//忽略追加操作
			context.Appender = null;

			if(context.Settings != null)
				context.Settings.SetFlags(BuilderSettingsFlags.IgnoreAppending);

			//调用基类同名方法
			return base.Build(context);
		}
	}
}
