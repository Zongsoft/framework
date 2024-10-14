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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Security
{
	/// <summary>
	/// 提供人机识别签发结果的格式化功能。
	/// </summary>
	/// <typeparam name="TContext">表示格式化上下文的类型。</typeparam>
	public interface ICaptchaFormatter<TContext>
	{
		/// <summary>格式化方法。</summary>
		/// <param name="context">指定的格式化上下文对象。</param>
		/// <param name="value">指定的待格式化的数据。</param>
		/// <param name="cancellation">异步操作的取消标记。</param>
		/// <returns>返回格式化后的结果。</returns>
		ValueTask<object> FormatAsync(TContext context, object value, CancellationToken cancellation = default);
	}
}