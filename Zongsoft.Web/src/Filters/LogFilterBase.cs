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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http.Extensions;

using Zongsoft.Services;
using Zongsoft.Services.Logging;

namespace Zongsoft.Web.Filters;

public abstract class LogFilterBase<TLog> : IAsyncActionFilter, IOrderedFilter where TLog : ILog
{
	#region 构造函数
	protected LogFilterBase() => this.Persistor = new LogPersistor<TLog>();
	#endregion

	#region 公共属性
	public virtual int Order => 0xFFFF;
	public ILogPersistor<TLog> Persistor { get; protected set; }
	#endregion

	#region 公共方法
	public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
	{
		//如果处于调试器模式，则不用日志处理
		if(!System.Diagnostics.Debugger.IsAttached)
		{
			//创建并组装日志实体
			var log = await this.CreateAsync(context);

			//进行日志写入
			if(log is not null)
				await this.PersistAsync(log);
		}

		await next();
	}
	#endregion

	#region 私有方法
	private async ValueTask<TLog> CreateAsync(ActionExecutingContext context)
	{
		var log = this.OnCreate(context);

		if(log == null)
			return log;

		log.Tracer = context.HttpContext.TraceIdentifier;
		log.Method = context.HttpContext.Request.Method;
		log.Url = context.HttpContext.Request.GetEncodedUrl();
		log.User = context.HttpContext.User;
		log.Scenario = context.HttpContext.User is Zongsoft.Security.CredentialPrincipal principal ? principal.Scenario : null;
		log.Timestamp = DateTime.UtcNow;

		var descriptor = GetDescriptor(context);
		if(descriptor != null)
		{
			log.Module = descriptor.Module;
			log.Target = descriptor.QualifiedName;
			log.Action = context.RouteData.Values.TryGetValue("action", out var value) ? value?.ToString() : context.ActionDescriptor.DisplayName;
		}

		await this.OnPopulateAsync(context, log);
		return log;
	}
	#endregion

	#region 虚拟方法
	protected virtual ValueTask PersistAsync(TLog log) => this.Persistor?.PersistAsync(log, default) ?? ValueTask.CompletedTask;
	protected virtual TLog OnCreate(ActionExecutingContext context) => typeof(TLog).IsAbstract ?
		Data.Model.Build<TLog>() :
		Activator.CreateInstance<TLog>();

	protected virtual async ValueTask OnPopulateAsync(ActionExecutingContext context, TLog log)
	{
		var populators = context.HttpContext.RequestServices.ResolveAll<ILogPopulator<ActionExecutingContext, TLog>>();

		foreach(var populator in populators)
			await populator.PopulateAsync(context, log);
	}
	#endregion

	private static ControllerServiceDescriptor GetDescriptor(ActionExecutingContext context)
	{
		return null;
	}
}
