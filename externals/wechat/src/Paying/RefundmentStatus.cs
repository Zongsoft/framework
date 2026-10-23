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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Wechat.Paying;

/// <summary>表示退款状态的枚举。</summary>
public enum RefundmentStatus
{
	/// <summary>退款成功</summary>
	[Components.Alias("Success")]
	Succeed,

	/// <summary>退款失败</summary>
	Failed,

	/// <summary>已关闭</summary>
	[Components.Alias("Closed")]
	Cancelled,

	/// <summary>处理中</summary>
	Processing,

	/// <summary>退款异常，需核对或处理收款账户。</summary>
	[Components.Alias("Abnormal")]
	Abnormal,

	/// <summary>未提供或无法识别的状态，必须查询确认。</summary>
	Unknown,
}
