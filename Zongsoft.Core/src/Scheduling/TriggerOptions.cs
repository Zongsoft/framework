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
