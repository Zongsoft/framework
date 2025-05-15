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
using System.Collections.Generic;

namespace Zongsoft.Components;

/// <summary>
/// 提供命令加载的功能。
/// </summary>
/// <remarks>
///		<para>对使用者的提醒：命令加载器不能重复使用，即不要把一个<see cref="ICommandLoader"/>实例赋予不同的用例，因为<seealso cref="IsLoaded"/>属性与不同的用例是无关的。</para>
/// </remarks>
public interface ICommandLoader
{
	/// <summary>获取一个值表示当前加载器是否已经加载完成。</summary>
	bool IsLoaded { get; }

	/// <summary>将命令加载到指定的命令树节点中。</summary>
	/// <param name="node">要挂载的命令树节点。</param>
	/// <remarks>
	///		<para>对实现者的提醒：应该确保该方法的实现是线程安全的。</para>
	/// </remarks>
	void Load(CommandNode node);
}
