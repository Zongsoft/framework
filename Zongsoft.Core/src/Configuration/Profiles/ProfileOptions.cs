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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles;

public class ProfileOptions
{
	#region 构造函数
	public ProfileOptions(params IEnumerable<IProfileDirective> directives) : this(true, directives) { }
	public ProfileOptions(bool reservedBlanks, params IEnumerable<IProfileDirective> directives)
	{
		this.ReservedBlanks = reservedBlanks;
		this.Directives = new ProfileDirectiveProvider(directives);
	}

	public ProfileOptions(params ReadOnlySpan<IProfileDirective> directives) : this(true, directives) { }
	public ProfileOptions(bool reservedBlanks, params ReadOnlySpan<IProfileDirective> directives)
	{
		this.ReservedBlanks = reservedBlanks;
		this.Directives = new ProfileDirectiveProvider(directives);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置一个值，指示是否保留空行。</summary>
	public bool ReservedBlanks { get; set; }

	/// <summary>获取或设置指令提供程序。</summary>
	public IProfileDirectiveProvider Directives { get; set; }
	#endregion
}
