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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Opc.Ua;
using Opc.Ua.Client;

namespace Zongsoft.Externals.Opc;

public sealed class OpcClientState
{
	#region 单例字段
	public static readonly OpcClientState Empty = new();
	#endregion

	#region 私有字段
	private readonly Session _session;
	#endregion

	#region 构造函数
	private OpcClientState() { }
	internal OpcClientState(Session session) => _session = session ?? throw new ArgumentNullException(nameof(session));
	#endregion

	#region 公共属性
	/// <summary>获取最近的心跳时间。</summary>
	public DateTime? Timestamp => _session?.LastKeepAliveTime;

	/// <summary>获取一个值，指示客户端是否活跃。</summary>
	public bool IsAlive => _session != null && _session.Connected && !_session.KeepAliveStopped;
	#endregion

	#region 重写方法
	public override string ToString() => this.IsAlive ? $"{this.Timestamp?.ToLocalTime()}" : $"Dead";
	#endregion
}