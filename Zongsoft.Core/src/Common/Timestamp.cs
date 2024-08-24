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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Common
{
	/// <summary>
	/// 提供时间戳相关功能的工具类。
	/// </summary>
	public class Timestamp
	{
		#region 单例字段
		/// <summary>表示Unix时间戳实例。</summary>
		public static readonly Timestamp Unix = new Timestamp(DateTime.UnixEpoch);

		/// <summary>表示以千禧年(公元2000年)为纪元的时间戳实例。</summary>
		public static readonly Timestamp Millennium = new Timestamp(new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc));
		#endregion

		#region 公共字段
		public readonly DateTime Epoch;
		#endregion

		#region 私有构造
		private Timestamp(DateTime epoch) => this.Epoch = epoch;
		#endregion

		#region 公共属性
		/// <summary>获取此刻的时间戳。</summary>
		public long Now { get => this.ToTimestamp(DateTime.UtcNow); }

		/// <summary>获取今天零时的时间戳。</summary>
		public long Today { get => this.ToTimestamp(DateTime.UtcNow.Date); }

		/// <summary>获取昨天零时的时间戳。</summary>
		public long Yesterday { get => this.ToTimestamp(DateTime.UtcNow.Date.AddDays(-1)); }
		#endregion

		#region 公共方法
		/// <summary>
		/// 将指定的日期时间转换为时间戳。
		/// </summary>
		/// <param name="datetime">指定的日期时间。</param>
		/// <param name="unit">待转换的时间戳单位。</param>
		/// <returns>返回的时间戳。</returns>
		public long ToTimestamp(DateTime datetime, TimestampUnit unit = TimestampUnit.Second)
		{
			return unit == TimestampUnit.Second ?
				(long)(datetime.ToUniversalTime() - this.Epoch).TotalSeconds :
				(long)(datetime.ToUniversalTime() - this.Epoch).TotalMilliseconds;
		}

		/// <summary>
		/// 将指定的时间戳转换为日期时间。
		/// </summary>
		/// <param name="timestamp">指定的时间戳。</param>
		/// <param name="unit">待转换的时间戳单位。</param>
		/// <returns>返回的日期时间。</returns>
		public DateTime ToDateTime(long timestamp, TimestampUnit unit = TimestampUnit.Second)
		{
			if(timestamp == 0)
				return this.Epoch;

			return unit == TimestampUnit.Second ?
				this.Epoch.AddSeconds(timestamp) :
				this.Epoch.AddMilliseconds(timestamp);
		}
		#endregion
	}

	/// <summary>
	/// 表示时间戳的单位。
	/// </summary>
	public enum TimestampUnit
	{
		/// <summary>秒</summary>
		Second,

		/// <summary>毫秒</summary>
		Millisecond,
	}
}
