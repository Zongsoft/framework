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

namespace Zongsoft.Scheduling.Samples.Models
{
	public class PlanModel
	{
		#region 构造函数
		public PlanModel()
		{
		}

		public PlanModel(uint planId, string name, string cron, bool enabled = true)
		{
			this.PlanId = planId;
			this.Name = string.IsNullOrEmpty(name) ? "Plan:" + planId.ToString() : name;
			this.CronExpression = cron;
			this.Enabled = enabled;
		}
		#endregion

		#region 公共属性
		public uint PlanId
		{
			get;
			set;
		}

		public string Name
		{
			get;
			set;
		}

		public bool Enabled
		{
			get;
			set;
		}

		public DateTime? EffectiveTime
		{
			get;
			set;
		}

		public DateTime? ExpirationTime
		{
			get;
			set;
		}

		public string CronExpression
		{
			get;
			set;
		}
		#endregion
	}
}
