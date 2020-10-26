/*
 *    _____                                ____
 *   /_   /  ____  ____  ____  ____ ____  / __/_
 *     / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ /_
 *    / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __  __/
 *   /____/\____/_/ /_/\__  /____/\____/_/ / /_
 *                    /____/               \__/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * The MIT License (MIT)
 * 
 * Copyright (C) 2018 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Scheduling.Samples
{
	public class MyScheduler : Scheduler
	{
		#region 成员字段
		private readonly ConcurrentDictionary<uint, MyHandler> _handlers;
		#endregion

		#region 构造函数
		public MyScheduler()
		{
			_handlers = new ConcurrentDictionary<uint, MyHandler>();
		}
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args)
		{
			//异步加载业务数据并生成调度任务
			this.Initialize();

			//调用基类同名方法
			base.OnStart(args);
		}
		#endregion

		#region 私有方法
		private void Initialize()
		{
			//获取可用的任务计划集
			var plans = this.GetPlans(200);

			foreach(var plan in plans)
			{
				//如果任务计划不可用则忽略该计划
				if(!plan.Enabled)
					continue;

				//根据当前计划的Cron表达式生成对应的触发器
				var trigger = this.GetCronTrigger(plan.CronExpression,
				                                  plan.EffectiveTime,
				                                  plan.ExpirationTime);

				//如果触发器生成成功，则将当前任务计划加入到调度器中
				if(trigger != null)
					this.Schedule(this.GetHandler(plan.PlanId), trigger);
			}

			//注意：如果上述生成任务计划不是异步方法，则不需要扫描(Scan)来重新生成调度计划
			this.Scan();
		}

		private IEnumerable<Models.PlanModel> GetPlans(int count)
		{
			for(int i = 0; i < count; i++)
			{
				var cron = (Common.Randomizer.GenerateInt32() % 6) switch
				{
					0 => "0 * * * * ?",                //每分钟来一发
					1 => "0 0/5 * * * ?",              //每5分钟来一发
					2 => "0 0,10,20,30,40,50 * * * ?", //每10分钟来一发
					3 => "0 0,30 * * * ?",             //每30分钟来一发
					4 => "0 0 0/2 * * ?",              //每2个小时来一发
					5 => "0 0 * ? * 1-5",              //工作日（周一至周五）的每小时来一发
					_ => "0 0 * * * ?",                //负数：每小时整点来一发
				};

				yield return new Models.PlanModel((uint)(i + 1), null, cron);
			};
		}

		private ITrigger GetCronTrigger(string expression, DateTime? effectiveTime, DateTime? expirationTime)
		{
			if(string.IsNullOrWhiteSpace(expression))
				return null;

			//如果生效时间小于当前则将其置空（以避免触发器的哈希值数量过多）
			if(effectiveTime.HasValue && effectiveTime.Value < DateTime.Now)
				effectiveTime = null;

			//如果过期时间比当前时间还早，则忽略它
			if(expirationTime.HasValue && expirationTime.Value < DateTime.Now)
				return null;

			try
			{
				//因为无效的Cron表达式可能会导致解析异常，所以需要捕获异常
				return Trigger.Cron(expression, expirationTime, effectiveTime);
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.Error(ex);
			}

			return null;
		}

		private MyHandler GetHandler(uint key)
		{
			return _handlers.GetOrAdd(key, id => new MyHandler(id));
		}
		#endregion
	}
}
