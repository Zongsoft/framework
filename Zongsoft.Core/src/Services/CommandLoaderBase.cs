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
using System.Collections.Generic;

namespace Zongsoft.Services;

public abstract class CommandLoaderBase : ICommandLoader
{
	#region 同步变量
	private readonly object _syncRoot = new object();
	#endregion

	#region 构造函数
	protected CommandLoaderBase() { }
	#endregion

	#region 成员字段
	private bool _isLoaded;
	#endregion

	#region 公共属性
	public bool IsLoaded => _isLoaded;
	#endregion

	#region 公共方法
	public void Load(CommandTreeNode node)
	{
		if(node == null || _isLoaded)
			return;

		lock(_syncRoot)
		{
			if(_isLoaded)
				return;

			_isLoaded = this.OnLoad(node);
		}
	}
	#endregion

	#region 抽象方法
	/// <summary>执行加载命令的实际操作。</summary>
	/// <param name="node">待加载的命令树节点。</param>
	/// <returns>如果加载成功则返回真(true)，否则返回假(false)。</returns>
	protected abstract bool OnLoad(CommandTreeNode node);
	#endregion
}
