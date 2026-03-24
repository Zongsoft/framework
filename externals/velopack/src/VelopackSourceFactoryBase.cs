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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Velopack;
using Velopack.Sources;

namespace Zongsoft.Externals.Velopack;

public abstract class VelopackSourceFactoryBase<TSource> : IVelopackSourceFactory where TSource : class, IUpdateSource
{
	#region 构造函数
	protected VelopackSourceFactoryBase(string name = null)
	{
		if(string.IsNullOrEmpty(name))
		{
			name = typeof(TSource).Name;

			if(name.Length > 6 && name.EndsWith("Source"))
				name = name[..^6];
		}

		this.Name = name;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	#endregion

	#region 公共方法
	IUpdateSource IVelopackSourceFactory.Create(string url, IReadOnlyDictionary<string, string> settings) => this.Create(url, settings);
	#endregion

	#region 抽象方法
	protected abstract TSource Create(string url, IReadOnlyDictionary<string, string> settings);
	#endregion
}
