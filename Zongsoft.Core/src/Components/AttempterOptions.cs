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
using System.ComponentModel;

namespace Zongsoft.Components;

/// <summary>
/// 表示尝试器的配置选项。
/// </summary>
public class AttempterOptions : IAttempterOptions
{
	#region 构造函数
	public AttempterOptions()
	{
		this.Limit = 3;
		this.Window = TimeSpan.FromSeconds(60);
		this.Period = TimeSpan.FromMinutes(10);
	}

	public AttempterOptions(int limit, TimeSpan window, TimeSpan period)
	{
		this.Limit = limit;
		this.Window = window;
		this.Period = period;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置尝试失败的阈值，零表示不限制。</summary>
	[DefaultValue(3)]
	public int Limit { get; set => field = Math.Max(value, 0); }

	/// <summary>获取或设置尝试失败的窗口期，默认为1分钟。</summary>
	[DefaultValue("0:1:0")]
	public TimeSpan Window { get; set => field = Zongsoft.Common.TimeSpanUtility.Max(value, TimeSpan.FromSeconds(1)); }

	/// <summary>获取或设置尝试失败超过阈值后的锁定时长，默认为10分钟。</summary>
	[DefaultValue("0:10:0")]
	public TimeSpan Period { get; set => field = Zongsoft.Common.TimeSpanUtility.Max(value, TimeSpan.FromSeconds(1)); }
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Limit}@{this.Window}({this.Period})";
	#endregion
}

public static class AttempterOptionsExtension
{
	public static bool HasLimit(this IAttempterOptions options, out int limit, out TimeSpan window, out TimeSpan period)
	{
		if(options == null)
		{
			limit = 0;
			window = TimeSpan.Zero;
			period = TimeSpan.Zero;

			return false;
		}

		limit = options.Limit;
		window = options.Window;
		period = options.Period;

		return limit > 0 && window > TimeSpan.Zero;
	}
}