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

namespace Zongsoft.Scheduling;

public class TriggerOptions : ITriggerOptions
{
	#region 构造函数
	public TriggerOptions(string id = null) => this.Identifier = id;
	#endregion

	#region 公共属性
	public string Identifier { get; set; }
	#endregion

	#region 嵌套子类
	public class Cron : TriggerOptions
	{
		private TimeZoneInfo _timezone;

		public Cron(string expression, TimeZoneInfo timezone = null) : this(null, expression, timezone) { }
		public Cron(string id, string expression, TimeZoneInfo timezone = null) : base(id)
		{
			this.Expression = expression;
			this.Timezone = timezone;
		}

		/// <summary>获取或设置 Cron 表达式文本。</summary>
		public string Expression { get; set; }

		/// <summary>获取或设置时区信息，默认为 <see cref="TimeZoneInfo.Utc"/> 时区。</summary>
		public TimeZoneInfo Timezone
		{
			get => _timezone ?? TimeZoneInfo.Utc;
			set => _timezone = value;
		}
	}

	public class Latency : TriggerOptions
	{
		public Latency(TimeSpan duration) => this.Duration = duration;
		public Latency(string id, TimeSpan duration) : base(id) => this.Duration = duration;

		/// <summary>获取或设置延迟的时长。</summary>
		public TimeSpan Duration { get; set; }
	}
	#endregion
}
