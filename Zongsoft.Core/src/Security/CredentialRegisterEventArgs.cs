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

namespace Zongsoft.Security;

public class CredentialRegisterEventArgs : EventArgs
{
	#region 构造函数
	public CredentialRegisterEventArgs(CredentialPrincipal principal, bool renewal = false)
	{
		this.IsRenewal = renewal;
		this.Principal = principal ?? throw new ArgumentNullException(nameof(principal));
	}
	#endregion

	#region 公共属性
	/// <summary>获取注册的凭证主体对象。</summary>
	public CredentialPrincipal Principal { get; }

	/// <summary>获取一个值，指示当前注册是否为续约引发。</summary>
	public bool IsRenewal { get; }
	#endregion
}
