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

namespace Zongsoft.Security;

/// <summary>
/// 表示用户验证失败的处理器。
/// </summary>
[Service(typeof(IAttempter))]
public class Attempter : IAttempter
{
	#region 公共属性
	[ServiceDependency("~", IsRequired = true)]
	public IDistributedCache Cache { get; set; }

	[Options("Security/Membership/Authentication/Attempter")]
	public Configuration.AttempterOptions Options { get; set; }
	IAttempterOptions IAttempter.Options
	{
		get => this.Options;
		set => this.Options = value as Configuration.AttempterOptions;
	}
	#endregion

	#region 公共方法
	public bool Verify(string identity, string @namespace = null)
	{
		if(string.IsNullOrEmpty(identity))
			return false;

		var option = this.Options;

		if(option == null || option.Threshold < 1)
			return true;

		return Common.Convert.TryConvertValue<int>(this.Cache.GetValue(GetCacheKey(identity, @namespace)), out var number) &&
		       number < option.Threshold;
	}

	public void Done(string identity, string @namespace = null)
	{
		if(string.IsNullOrEmpty(identity))
			return;

		this.Cache.Remove(GetCacheKey(identity, @namespace));
	}

	public bool Fail(string identity, string @namespace = null)
	{
		if(string.IsNullOrEmpty(identity))
			return false;

		if(this.Cache is not ISequence sequence)
			throw new InvalidOperationException($"The cache of authentication failover does not support the increment(ISequence) operation.");

		//获取验证失败的阈值和锁定时长
		this.GetAttempts(out var threshold, out var window, out var period);

		if(threshold < 1 || window == TimeSpan.Zero)
			return false;

		var KEY = GetCacheKey(identity, @namespace);
		var attempts = sequence.Increase(KEY);

		if(attempts < threshold)
			this.Cache.SetExpiry(KEY, window);
		else if(attempts == threshold)
			this.Cache.SetExpiry(KEY, period);

		return attempts >= threshold;
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void GetAttempts(out int threshold, out TimeSpan window, out TimeSpan period)
	{
		threshold = 3;
		window = TimeSpan.FromMinutes(1);
		period = TimeSpan.FromMinutes(60);

		var option = this.Options;

		if(option != null)
		{
			threshold = Math.Max(option.Threshold, 1);

			if(option.Window > TimeSpan.Zero)
				window = option.Window;

			if(option.Period > TimeSpan.Zero)
				period = option.Period;
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
