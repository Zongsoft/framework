﻿/*
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

namespace Zongsoft.Security
{
	/// <summary>
	/// 提供秘密（验证码）发送功能的接口。
	/// </summary>
	public interface ISecretIssuer
	{
		/// <summary>获取支持的发送通道标识集。</summary>
		string[] Channels { get; }

		/// <summary>
		/// 获取默认的通道标识。
		/// </summary>
		/// <param name="destination">指定的目的标识。</param>
		/// <returns>返回指定目的对应的默认通道。</returns>
		string GetChannel(string destination);

		/// <summary>
		/// 发送秘密（验证码）到指定的目的。
		/// </summary>
		/// <param name="destination">指定的验证码接受目的。</param>
		/// <param name="template">指定的模板标识。</param>
		/// <param name="secret">指定的秘密（验证码）。</param>
		/// <param name="channel">指定的通道标识。</param>
		void Issue(string destination, string template, string secret, string channel = null);
	}
}