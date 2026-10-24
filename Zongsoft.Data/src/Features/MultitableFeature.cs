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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Zongsoft.Data.Features;

/// <summary>声明在一条语句中写入多个表的能力。</summary>
public interface IMultitableFeature
{
	/// <summary>获取一个值，指示是否支持多表更新。</summary>
	bool CanUpdate { get; }

	/// <summary>获取一个值，指示是否支持多表删除。</summary>
	bool CanDelete { get; }
}

/// <summary>提供多表更新和删除能力的不可变声明。</summary>
public class MultitableFeature : IMultitableFeature
{
	/// <summary>初始化多表写入能力。</summary>
	/// <param name="deletable">指示是否支持多表删除。</param>
	/// <param name="updatable">指示是否支持多表更新。</param>
	public MultitableFeature(bool deletable, bool updatable)
	{
		this.CanDelete = deletable;
		this.CanUpdate = updatable;
	}

	/// <summary>获取一个值，指示是否支持多表删除。</summary>
	public bool CanDelete { get; }

	/// <summary>获取一个值，指示是否支持多表更新。</summary>
	public bool CanUpdate { get; }
}
