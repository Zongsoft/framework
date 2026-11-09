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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Zongsoft.Diagnostics;

namespace Zongsoft.Web.Filters;

public abstract class LoggingFilterBase<TLog> : IAsyncActionFilter, IOrderedFilter where TLog : ILog
{
	#region 公共属性
	public virtual int Order => 0xFFFF;
	#endregion

	#region 公共方法
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		//如果处于调试器模式，则不用日志处理
		if(!System.Diagnostics.Debugger.IsAttached)
		{
			//创建日志实体
			var log = this.CreateLog(context);

			//进行日志写入
			if(log is not null)
				await Logging.LogAsync(log);
		}

		await next();
	}
	#endregion

	#region 虚拟方法
	protected abstract TLog CreateLog(ActionExecutingContext context);
	#endregion
}
