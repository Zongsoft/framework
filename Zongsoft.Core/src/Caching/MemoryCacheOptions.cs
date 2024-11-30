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

namespace Zongsoft.Caching;

public class MemoryCacheOptions
{
	#region 成员字段
	private int _limit;
	private TimeSpan _frequency;
	#endregion

	#region 构造函数
	public MemoryCacheOptions() => _frequency = TimeSpan.FromSeconds(60);
	public MemoryCacheOptions(TimeSpan frequency, int limit = 0)
	{
		this.ScanFrequency = frequency;
		this.CountLimit = limit;
	}
	#endregion

	#region 公共属性
	public int CountLimit
	{
		get => _limit;
		set => _limit = value > 0 ? value : 0;
	}

	public TimeSpan ScanFrequency
	{
		get => _frequency;
		set => _frequency = value.Ticks > TimeSpan.TicksPerSecond ? value : TimeSpan.FromSeconds(1);
	}
	#endregion

	#region 公共方法
	public bool IsLimit(out int limit)
	{
		limit = _limit;
		return limit > 0;
	}
	#endregion
}
