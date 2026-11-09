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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
	private readonly ISession _session;
	#endregion

	#region 构造函数
	private OpcClientState() { }
	internal OpcClientState(ISession session) => _session = session ?? throw new ArgumentNullException(nameof(session));
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