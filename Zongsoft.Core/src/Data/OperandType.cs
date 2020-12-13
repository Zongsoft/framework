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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示操作数类型的枚举。
	/// </summary>
	public enum OperandType
	{
		/// <summary>加号</summary>
		Add,
		/// <summary>减号</summary>
		Subtract,
		/// <summary>乘号(*)</summary>
		Multiply,
		/// <summary>除号(/)</summary>
		Divide,
		/// <summary>取模(%)</summary>
		Modulo,

		/// <summary>负号</summary>
		Negate,
		/// <summary>逻辑非/位取反</summary>
		Not,

		/// <summary>逻辑与/位与</summary>
		And,
		/// <summary>逻辑或/位或</summary>
		Or,
		/// <summary>位异或</summary>
		Xor,

		/// <summary>字段</summary>
		Field,
		/// <summary>常量</summary>
		Constant,
		/// <summary>函数</summary>
		Function,
	}
}
