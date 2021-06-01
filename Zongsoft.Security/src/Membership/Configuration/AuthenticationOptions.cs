/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

namespace Zongsoft.Security.Membership.Configuration
{
	/// <summary>
	/// 表示身份验证的配置选项。
	/// </summary>
	public class AuthenticationOptions
	{
		#region 静态字段
		public static readonly TimeSpan DefaultPeriod = TimeSpan.FromHours(4);
		#endregion

		#region 构造函数
		public AuthenticationOptions()
		{
			this.Scheme = string.Empty;
			this.Period = DefaultPeriod;
			this.Expiration = new Collections.NamedCollection<ExpirationScenario>(scenario => scenario.Name);
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置默认的安全方案。</summary>
		public string Scheme { get; set; }

		/// <summary>获取或设置凭证的默认有效期时长。</summary>
		[DefaultValue("4:0:0")]
		public TimeSpan Period { get; set; }

		/// <summary>获取或设置恶意检测器的配置项。</summary>
		public AttempterOptions Attempter { get; set; }

		/// <summary>获取凭证过期配置集。</summary>
		public Collections.INamedCollection<ExpirationScenario> Expiration { get; }
		#endregion

		#region 公共方法
		public TimeSpan GetPeriod(string scenario)
		{
			var period = DefaultPeriod;

			if(string.IsNullOrEmpty(scenario))
				period = this.Period;
			else if(this.Expiration.TryGet(scenario, out var expiration))
				period = expiration.Period;

			if(period.TotalMinutes > 1)
				return period;
			else
				return DefaultPeriod;
		}
		#endregion

		#region 嵌套子类
		/// <summary>
		/// 表示以凭证场景为依据的有效期配置项。
		/// </summary>
		public class ExpirationScenario
		{
			/// <summary>获取凭证场景名。</summary>
			public string Name { get; set; }

			/// <summary>获取或设置凭证的有效期时长。</summary>
			public TimeSpan Period { get; set; }
		}
		#endregion
	}
}
