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
/// 提供序号递增(减)基础功能的接口。
/// </summary>
public interface ISequenceBase
{
	#region 常量定义
	private const int DEFAULT_SEED = 0;
	private const int DEFAULT_INTERVAL = 1;
	#endregion

	/// <summary>递减指定序列号的数值，默认递减步长为<c>1</c>。</summary>
	/// <param name="key">指定递减的序列号键。</param>
	/// <param name="interval">指定的序列号递减的步长数，如果为零则表示获取当前值，而不会引发递减。</param>
	/// <param name="seed">初始化的种子数，如果指定名称的序列号不存在，则创建指定名称的序列号，并使用该种子数作为该序列号的初始值。</param>
	/// <returns>返回递减后的序列号数值。</returns>
	long Decrease(string key, int interval = DEFAULT_INTERVAL, int seed = DEFAULT_SEED);

	/// <summary>递减指定序列号的数值，默认递减步长为<c>1</c>。</summary>
	/// <param name="key">指定递减的序列号键。</param>
	/// <param name="interval">指定的序列号递减的步长数，如果为零则表示获取当前值，而不会引发递减。</param>
	/// <param name="seed">初始化的种子数，如果指定名称的序列号不存在，则创建指定名称的序列号，并使用该种子数作为该序列号的初始值。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回递减后的序列号数值。</returns>
	ValueTask<long> DecreaseAsync(string key, int interval = DEFAULT_INTERVAL, int seed = DEFAULT_SEED, CancellationToken cancellation = default);

	/// <summary>递增指定序列号的数值，默认递增步长为<c>1</c>。</summary>
	/// <param name="key">指定递增的序列号键。</param>
	/// <param name="interval">指定的序列号递增的步长数，如果为零则表示获取当前值，而不会引发递增。</param>
	/// <param name="seed">初始化的种子数，如果指定名称的序列号不存在，则创建指定名称的序列号，并使用该种子数作为该序列号的初始值。</param>
	/// <returns>返回递增后的序列号数值。</returns>
	long Increase(string key, int interval = DEFAULT_INTERVAL, int seed = DEFAULT_SEED);

	/// <summary>递增指定序列号的数值，默认递增步长为<c>1</c>。</summary>
	/// <param name="key">指定递增的序列号键。</param>
	/// <param name="interval">指定的序列号递增的步长数，如果为零则表示获取当前值，而不会引发递增。</param>
	/// <param name="seed">初始化的种子数，如果指定名称的序列号不存在，则创建指定名称的序列号，并使用该种子数作为该序列号的初始值。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回递增后的序列号数值。</returns>
	ValueTask<long> IncreaseAsync(string key, int interval = DEFAULT_INTERVAL, int seed = DEFAULT_SEED, CancellationToken cancellation = default);

	/// <summary>重置指定的序列号数值，如果指定键的序列号不存在则创建它。</summary>
	/// <param name="key">指定要重置的序列号键。</param>
	/// <param name="value">指定要重置的序列号当前值，默认为零。</param>
	void Reset(string key, int value = DEFAULT_SEED);

	/// <summary>重置指定的序列号数值，如果指定键的序列号不存在则创建它。</summary>
	/// <param name="key">指定要重置的序列号键。</param>
	/// <param name="value">指定要重置的序列号当前值，默认为零。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	ValueTask ResetAsync(string key, int value = DEFAULT_SEED, CancellationToken cancellation = default);
}
