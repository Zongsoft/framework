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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

namespace Zongsoft.Data.Features;

/// <summary>声明数据库事务的行为。</summary>
public interface ITransactionFeature
{
	/// <summary>获取一个值，指示是否抑制数据库事务。</summary>
	bool Suppressed { get; }
}

/// <summary>提供数据库事务行为的不可变声明。</summary>
public sealed class TransactionFeature : ITransactionFeature
{
	/// <summary>获取不抑制数据库事务的共享实例。</summary>
	public static readonly TransactionFeature Instance = new();

	/// <summary>初始化数据库事务行为。</summary>
	/// <param name="suppressed">指示是否抑制数据库事务，默认为否。</param>
	public TransactionFeature(bool suppressed = false) => this.Suppressed = suppressed;

	/// <summary>获取一个值，指示是否抑制数据库事务。</summary>
	public bool Suppressed { get; }
}
