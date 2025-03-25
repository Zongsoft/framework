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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;

namespace Zongsoft.Components;

public class Attempter : IAttempter
{
	#region 成员字段
	private IDistributedCache _cache;
	private IAttempterOptions _options;
	#endregion

	#region 构造函数
	public Attempter(IAttempterOptions options = null) => _options = options ?? new AttempterOptions();
	#endregion

	#region 公共属性
	public IDistributedCache Cache
	{
		get => _cache ??= ApplicationContext.Current?.Services.ResolveRequired<IServiceProvider<IDistributedCache>>().GetService();
		set => _cache = value ?? throw new ArgumentNullException(nameof(value));
	}

	public IAttempterOptions Options
	{
		get => _options;
		set => _options = value ?? throw new ArgumentNullException(nameof(value));
	}
	#endregion

	#region 公共方法
	public async ValueTask<bool> CheckAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return false;

		var option = this.Options;
		if(option == null || option.Limit < 1)
			return true;

		var value = await this.Cache.GetValueAsync(GetCacheKey(key), cancellation);
		return Common.Convert.TryConvertValue<int>(value, out var limit) && limit < option.Limit;
	}

	public ValueTask<bool> DoneAsync(string key, CancellationToken cancellation = default)
	{
		if(key != null && key.Length > 0)
			return this.Cache.RemoveAsync(GetCacheKey(key), cancellation);

		return ValueTask.FromResult(false);
	}

	public async ValueTask<bool> FailAsync(string key, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return false;

		if(this.Cache is not ISequence sequence)
			throw new InvalidOperationException($"The cache of authentication failover does not support the increment(ISequence) operation.");

		//获取验证失败的阈值和锁定时长
		this.GetAttempts(out var limit, out var window, out var period);

		if(limit < 1 || window == TimeSpan.Zero)
			return false;

		var KEY = GetCacheKey(key);
		var attempts = await sequence.IncreaseAsync(KEY, cancellation: cancellation);

		if(attempts < limit)
			await this.Cache.SetExpiryAsync(KEY, window, cancellation);
		else if(attempts == limit)
			await this.Cache.SetExpiryAsync(KEY, period, cancellation);

		return attempts >= limit;
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void GetAttempts(out int limit, out TimeSpan window, out TimeSpan period)
	{
		limit = 3;
		window = TimeSpan.FromMinutes(1);
		period = TimeSpan.FromMinutes(60);

		var option = this.Options;

		if(option != null)
		{
			limit = Math.Max(option.Limit, 1);

			if(option.Window > TimeSpan.Zero)
				window = option.Window;

			if(option.Period > TimeSpan.Zero)
				period = option.Period;
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static string GetCacheKey(string key)
	{
		const string KEY_PREFIX = "Zongsoft.Security.Attempts";
		return $"{KEY_PREFIX}:{key.ToLowerInvariant().Trim()}";
	}
	#endregion
}
