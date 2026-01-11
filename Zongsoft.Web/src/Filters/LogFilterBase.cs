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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Zongsoft.Diagnostics;

namespace Zongsoft.Web.Filters;

public abstract class LogFilterBase<TLog> : IAsyncActionFilter, IOrderedFilter where TLog : ILog
{
	#region 公共属性
	public virtual int Order => 0xFFFF;
	#endregion

	#region 公共方法
	public Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		//如果处于调试器模式，则不用日志处理
		if(!System.Diagnostics.Debugger.IsAttached)
		{
			//创建日志实体
			var log = this.CreateLog(context);

			//进行日志写入
			if(log is not null)
				return Logging.LogAsync(log).AsTask().ContinueWith(task => next());
		}

		return next();
	}
	#endregion

	#region 虚拟方法
	protected abstract TLog CreateLog(ActionExecutingContext context);
	#endregion
}
