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

namespace Zongsoft.Scheduling.Samples
{
	public class MyScheduler : Scheduler<uint, Models.PlanModel>
	{
		#region 构造函数
		public MyScheduler() { }
		#endregion

		#region 重写方法
		protected override void Initialize(IEnumerable<KeyValuePair<uint, Models.PlanModel>> schedulars)
		{
			static IEnumerable<KeyValuePair<uint, Models.PlanModel>> Mock(int count)
			{
				for(int i = 0; i < count; i++)
				{
					yield return new KeyValuePair<uint, Models.PlanModel>((uint)(i + 1), new Models.PlanModel((uint)(i + 1), null, GenerateCron()));
				}
			}

			base.Initialize(schedulars ?? Mock(200));
		}

		protected override Models.PlanModel GetData(uint key)
		{
			return new Models.PlanModel(key, null, GenerateCron());
		}

		protected override IHandler GetHandler(Models.PlanModel data)
		{
			return MyHandler.Default;
		}

		protected override ITrigger GetTrigger(Models.PlanModel data)
		{
			if(data == null || string.IsNullOrWhiteSpace(data.CronExpression))
				return null;

			try
			{
				//因为无效的Cron表达式可能会导致解析异常，所以需要捕获异常
				return Trigger.Cron(data.CronExpression, data.ExpirationTime, data.EffectiveTime, $"{data.PlanId}:{data.CronExpression}");
			}
			catch(Exception ex)
			{
				Zongsoft.Diagnostics.Logger.Error(ex);
			}

			return null;
		}
		#endregion

		#region 私有方法
		private static string GenerateCron()
		{
			return (Common.Randomizer.GenerateInt32() % 9) switch
			{
				0 => "0 * * * * ?",                //每1分钟来一发
				1 => "0 0/5 * * * ?",              //每5分钟来一发
				2 => "0 0,10,20,30,40,50 * * * ?", //每10分钟来一发
				3 => "0 0,30 * * * ?",             //每30分钟来一发
				4 => "0 0 0/2 * * ?",              //每2个小时来一发
				5 => "0 0 * ? * 1-5",              //周一至周五的每小时来一发
				6 => "0 0 0 L 6 ?",                //6月的最后一天来一发
				7 => "0 0 0 1 1 ?",                //1月1号来一发
				8 => "0 0 0 31 12 ?",              //12月31号来一发
				_ => "0 0 * * * ?",                //每1小时来一发
			};
		}
		#endregion
	}
}
