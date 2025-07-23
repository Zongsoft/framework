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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Common;

/// <summary>
/// 表示条件判断的接口。
/// </summary>
public interface IPredication<in T> : IPredication
{
	/// <summary>确定指定对象是否符合某种条件。</summary>
	/// <param name="argument">指定的条件参数对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果符合某种条件则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> PredicateAsync(T argument, CancellationToken cancellation = default);

	/// <summary>确定指定对象是否符合某种条件。</summary>
	/// <param name="argument">指定的条件参数对象。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果符合某种条件则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> PredicateAsync(T argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
