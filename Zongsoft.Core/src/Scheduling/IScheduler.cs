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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Components;

namespace Zongsoft.Scheduling
{
	/// <summary>
	/// 表示作业任务调度器的接口。
	/// </summary>
	public interface IScheduler
	{
		/// <summary>调度作业任务。</summary>
		/// <param name="name">任务名称或处理程序标识。</param>
		/// <param name="options">任务触发选项。</param>
		/// <returns>返回调度成功的作业标识。</returns>
		string Schedule(string name, ITriggerOptions options = null);

		/// <summary>调度作业任务。</summary>
		/// <typeparam name="TParameter">任务参数类型。</typeparam>
		/// <param name="name">任务名称或处理程序标识。</param>
		/// <param name="parameter">任务处理参数。</param>
		/// <param name="options">任务触发选项。</param>
		/// <returns>返回调度成功的作业标识。</returns>
		string Schedule<TParameter>(string name, TParameter parameter, ITriggerOptions options = null);

		/// <summary>触发作业调度。</summary>
		/// <param name="identifier">要触发的作业标识。</param>
		/// <returns>如果触发成功则返回真(True)，否则返回假(False)。</returns>
		bool Reschedule(string identifier);

		/// <summary>取消作业调度。</summary>
		/// <param name="identifier">要取消的作业标识。</param>
		/// <returns>如果取消作业成功则返回真(True)，否则返回假(False)。</returns>
		bool Unschedule(string identifier);
	}
}
