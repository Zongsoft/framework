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
 * Copyright (C) 2018 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Scheduling.Cron.
 *
 * Zongsoft.Scheduling.Cron is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.Scheduling.Cron is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.Scheduling.Cron; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Collections.Concurrent;

namespace Zongsoft.Scheduling
{
	public class CronTrigger : ITrigger, IEquatable<ITrigger>
	{
		#region 单例字段
		public static readonly ITriggerBuilder Builder = new CronTriggerBuilder();
		#endregion

		#region 成员字段
		private Cronos.CronExpression _expression;
		#endregion

		#region 私有构造
		private CronTrigger(string expression, DateTime? expiration = null, DateTime? effective = null)
		{
			try
			{
				_expression = Cronos.CronExpression.Parse(expression, Cronos.CronFormat.IncludeSeconds);
			}
			catch(Exception ex)
			{
				throw new ArgumentException($"The specified '{expression}' is an illegal Cron expression.", ex);
			}

			this.Expression = _expression.ToString();
			this.ExpirationTime = expiration;
			this.EffectiveTime = effective;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取触发器的Cron表达式。
		/// </summary>
		public string Expression
		{
			get;
		}

		/// <summary>
		/// 获取或设置触发器的生效时间。
		/// </summary>
		public DateTime? EffectiveTime
		{
			get;
			set;
		}

		/// <summary>
		/// 获取或设置触发器的截止时间。
		/// </summary>
		public DateTime? ExpirationTime
		{
			get;
			set;
		}
		#endregion

		#region 公共方法
		public DateTime? GetNextOccurrence(bool inclusive = false)
		{
			var now = this.Now();

			if(this.EffectiveTime.HasValue && now < this.EffectiveTime.Value)
				return null;
			if(this.ExpirationTime.HasValue && now > this.ExpirationTime.Value)
				return null;

			return _expression.GetNextOccurrence(now, inclusive);
		}

		public DateTime? GetNextOccurrence(DateTime origin, bool inclusive = false)
		{
			var now = this.Now(origin);

			if(this.EffectiveTime.HasValue && now < this.EffectiveTime.Value)
				return null;
			if(this.ExpirationTime.HasValue && now > this.ExpirationTime.Value)
				return null;

			return _expression.GetNextOccurrence(this.Now(origin), inclusive);
		}
		#endregion

		#region 重写方法
		public bool Equals(ITrigger other)
		{
			return (other is CronTrigger cron) &&
				cron._expression.Equals(_expression) &&
				this.EffectiveTime == other.EffectiveTime &&
				this.ExpirationTime == other.ExpirationTime;
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj as CronTrigger);
		}

		public override int GetHashCode()
		{
			var code = _expression.GetHashCode();

			if(this.EffectiveTime.HasValue)
				code ^= this.EffectiveTime.Value.GetHashCode();
			if(this.ExpirationTime.HasValue)
				code ^= this.ExpirationTime.Value.GetHashCode();

			return code;
		}

		public override string ToString()
		{
			if(this.EffectiveTime == null && this.ExpirationTime == null)
				return "Cron: " + this.Expression;
			else
				return "Cron: " + this.Expression + " (" +
					(this.EffectiveTime.HasValue ? this.EffectiveTime.ToString() : "?") + " ~ " +
					(this.ExpirationTime.HasValue ? this.ExpirationTime.ToString() : "?") + ")";
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private DateTime Now(DateTime? timestamp = null)
		{
			return new DateTime(timestamp.HasValue ? timestamp.Value.Ticks : DateTime.Now.Ticks, DateTimeKind.Utc);
		}
		#endregion

		#region 构建器类
		private class CronTriggerBuilder : ITriggerBuilder
		{
			public ITrigger Build(string expression, DateTime? expiration = null, DateTime? effective = null)
			{
				if(string.IsNullOrWhiteSpace(expression))
					throw new ArgumentNullException(nameof(expression));

				return new CronTrigger(expression, expiration, effective);
			}
		}
		#endregion
	}
}
