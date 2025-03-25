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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

namespace Zongsoft.Security.Configuration
{
	/// <summary>
	/// 表示恶意检测器的配置选项。
	/// </summary>
	public class AttempterOptions : IAttempterOptions
	{
		public AttempterOptions()
		{
			this.Threshold = 3;
			this.Window = TimeSpan.FromMinutes(1);
			this.Period = TimeSpan.FromHours(1);
		}

		/// <summary>获取或设置验证失败的阈值，零表示不限制。</summary>
		[DefaultValue(3)]
		public int Threshold { get; set; }

		/// <summary>获取或设置验证失败的窗口期，默认为1分钟。</summary>
		[DefaultValue("0:1:0")]
		public TimeSpan Window { get; set; }

		/// <summary>获取或设置验证失败超过指定的阈值后锁定时长，默认为60分钟。</summary>
		[DefaultValue("1:0:0")]
		public TimeSpan Period { get; set; }
	}
}
