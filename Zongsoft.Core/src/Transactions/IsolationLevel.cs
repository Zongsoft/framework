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

namespace Zongsoft.Transactions
{
	/// <summary>
	/// 表示事务的隔离级别。
	/// </summary>
	public enum IsolationLevel
	{
		/// <summary>可以在事务期间读取和修改可变数据，可以进行脏读。</summary>
		ReadUncommitted = 0,

		/// <summary>可以在事务期间读取可变数据，但是不可以修改它。在正在读取数据时保持共享锁，以避免脏读，但是在事务结束之前可以更改数据，从而导致不可重复的读取或幻像数据。</summary>
		ReadCommitted,

		/// <summary>可以在事务期间读取可变数据，但是不可以修改。可以在事务期间添加新数据。防止不可重复的读取，但是仍可以有幻像数据。</summary>
		RepeatableRead,

		/// <summary>可以在事务期间读取可变数据，但是不可以修改，也不可以添加任何新数据。</summary>
		Serializable,
	}
}
