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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Communication
{
	/// <summary>
	/// 提供发送器模板参数转换功能的接口。
	/// </summary>
	public interface ITransmitterArgumenter
	{
		/// <summary>将指定的数据转换为模板参数。</summary>
		/// <param name="transmitter">指定的发送器对象。</param>
		/// <param name="channel">指定的发送通道。</param>
		/// <param name="template">指定的模板名称。</param>
		/// <param name="data">指定的待转换的参数对象。</param>
		/// <param name="parameters">指定的参数集合。</param>
		/// <returns>返回转换后的对象。</returns>
		object GetArgument(ITransmitter transmitter, string channel, string template, object data, Collections.Parameters parameters);
	}
}