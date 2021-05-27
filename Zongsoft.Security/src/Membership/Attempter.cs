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

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 表示用户验证失败的处理器。
	/// </summary>
	[Service(typeof(IAttempter))]
	public class Attempter : IAttempter
	{
		#region 公共属性
		[ServiceDependency]
		public IServiceAccessor<ICache> Cache { get; set; }

		[Options("Security/Membership/Authentication/Attempter")]
		public Configuration.AttempterOptions Options
		{
			get; set;
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 校验指定用户是否可以继续验证。
		/// </summary>
		/// <param name="identity">指定待验证的用户标识。</param>
		/// <param name="scene">表示验证操作的场景。</param>
		/// <returns>如果校验成功则返回真(True)，否则返回假(False)。</returns>
		public bool Verify(string identity, string @namespace = null)
		{
			var option = this.Options;

			if(option == null || option.Threshold < 1)
				return true;

			var cache = this.Cache.Value ?? throw new InvalidOperationException("Missing the required cache.");

			if(cache == null)
				return true;

			return Zongsoft.Common.Convert.TryConvertValue<int>(cache.GetValue(GetCacheKey(identity, @namespace)), out var number) &&
			       number < option.Threshold;
		}

		/// <summary>
		/// 验证成功方法。
		/// </summary>
		/// <param name="identity">指定验证成功的用户标识。</param>
		/// <param name="scene">表示验证操作的场景。</param>
		public void Done(string identity, string @namespace = null)
		{
			var cache = this.Cache.Value ?? throw new InvalidOperationException("Missing the required cache.");

			if(cache != null)
				cache.Remove(GetCacheKey(identity, @namespace));
		}

		/// <summary>
		/// 验证失败方法。
		/// </summary>
		/// <param name="identity">指定验证失败的用户标识。</param>
		/// <param name="scene">表示验证操作的场景。</param>
		/// <returns>返回验证失败是否超过阈值，如果返回真(True)则表示失败次数超过阈值。</returns>
		public bool Fail(string identity, string @namespace = null)
		{
			var cache = this.Cache.Value ?? throw new InvalidOperationException("Missing the required cache.");

			if(cache is not ISequence sequence)
				throw new InvalidOperationException($"The cache of authentication failover does not support the increment(ISequence) operation.");

			//获取验证失败的阈值和锁定时长
			this.GetAttempts(out var threshold, out var window);

			if(threshold < 1 || window == TimeSpan.Zero)
				return false;

			var KEY = GetCacheKey(identity, @namespace);
			var attempts = sequence.Increment(KEY);

			//如果失败计数器为新增（即递增结果为零或1），或者失败计数器到达限制数；
			//则更新失败计数器的过期时长为指定的锁定时长。
			if(attempts == 0 || attempts == 1 || attempts == threshold)
				cache.SetExpiry(KEY, window);

			return attempts >= threshold;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void GetAttempts(out int threshold, out TimeSpan window)
		{
			threshold = 3;
			window = TimeSpan.FromHours(1);

			var option = this.Options;

			if(option != null)
			{
				threshold = option.Threshold;
				window = option.Window;
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetCacheKey(string identity, string @namespace)
		{
			const string KEY_PREFIX = "Zongsoft.Security.Attempts";

			return string.IsNullOrEmpty(@namespace) ?
				$"{KEY_PREFIX}:{identity.ToLowerInvariant().Trim()}" :
				$"{KEY_PREFIX}:{identity.ToLowerInvariant().Trim()}!{@namespace.ToLowerInvariant().Trim()}";
		}
		#endregion
	}
}
