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
 * This file is part of Zongsoft.Scheduling library.
 *
 * The Zongsoft.Scheduling is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Scheduling is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Scheduling library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Scheduling
{
	/// <summary>
	/// 表示时间触发器的接口。
	/// </summary>
	public interface ITrigger : IEquatable<ITrigger>
	{
		#region 属性定义
		/// <summary>获取触发器的表达式文本。</summary>
		string Expression { get; }

		/// <summary>获取或设置触发器的生效时间。</summary>
		DateTime? EffectiveTime { get; set; }

		/// <summary>获取或设置触发器的截止时间。</summary>
		DateTime? ExpirationTime { get; set; }

		/// <summary>获取或设置触发器的描述文本。</summary>
		string Description { get; set; }
		#endregion

		#region 方法定义
		/// <summary>
		/// 计算触发器的下次触发时间，如果结果为空(null)表示不再触发。
		/// </summary>
		/// <param name="inclusive">指定一个值，本次计算是否包含当前时间点。</param>
		/// <returns>返回下次触发的时间，如果为空(null)则表示不再触发。</returns>
		DateTime? GetNextOccurrence(bool inclusive = false);

		/// <summary>
		/// 计算触发器的下次触发时间，如果结果为空(null)表示不再触发。
		/// </summary>
		/// <param name="origin">指定开始计算的起始时间。</param>
		/// <param name="inclusive">指定一个值，本次计算是否包含<paramref name="origin"/>参数指定的起始时间。</param>
		/// <returns>返回下次触发的时间，如果为空(null)则表示不再触发。</returns>
		DateTime? GetNextOccurrence(DateTime origin, bool inclusive = false);
		#endregion
	}
}
