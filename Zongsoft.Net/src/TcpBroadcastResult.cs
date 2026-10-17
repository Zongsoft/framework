/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Net;

/// <summary>广播的本地发送结果，不表示远端接收或业务确认。</summary>
/// <param name="Succeeded">成功完成本地提交的连接数。</param>
/// <param name="Failed">发送失败的连接数，不包含 <see cref="TcpError.SendRejected" /> 对应的容量拒绝。</param>
/// <param name="Rejected">以 <see cref="TcpError.SendRejected" /> 拒绝发送的连接数。</param>
public readonly record struct TcpBroadcastResult(int Succeeded, int Failed, int Rejected);
