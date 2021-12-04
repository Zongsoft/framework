/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.ComponentModel;

namespace Zongsoft.Externals.Wechat.Paying
{
	/// <summary>
	/// 表示支付方式的枚举。
	/// </summary>
	public enum PaymentKind
	{
		/// <summary>公众号支付</summary>
		[Zongsoft.ComponentModel.Alias("JSAPI")]
		Web,

		/// <summary>扫码支付</summary>
		Native,

		/// <summary>APP支付</summary>
		App,

		/// <summary>付款码支付</summary>
		[Zongsoft.ComponentModel.Alias("MICROPAY")]
		Paycode,

		/// <summary>H5支付</summary>
		[Zongsoft.ComponentModel.Alias("MWEB")]
		Mobile,

		/// <summary>刷脸支付</summary>
		[Zongsoft.ComponentModel.Alias("FACEPAY")]
		Facepay,
	}
}
