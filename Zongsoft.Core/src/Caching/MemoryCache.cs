﻿/*
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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Caching.Memory;

namespace Zongsoft.Caching;

public class MemoryCache : IDisposable
{
	#region 单例字段
	public static readonly MemoryCache Shared = new();
	#endregion

	#region 事件声明
	public event EventHandler<CacheLimitedEventArgs> Limited;
	public event EventHandler<CacheEvictedEventArgs> Evicted;
	#endregion

	#region 成员字段
	private readonly MemoryCacheOptions _options;
	private Microsoft.Extensions.Caching.Memory.MemoryCache _cache;
	#endregion

	#region 构造函数
	public MemoryCache() : this(TimeSpan.FromSeconds(60)) { }
	public MemoryCache(TimeSpan frequency, int limit = 0)
	{
		_options = new MemoryCacheOptions(frequency, limit);
		_cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()
		{
			ExpirationScanFrequency = _options.ScanFrequency,
		});
	}
	#endregion

	#region 公共属性
	public int Count => _cache.Count;
	public MemoryCacheOptions Options => _options;

	#if NET9_0_OR_GREATER
	public IEnumerable<object> Keys => _cache.Keys;
	#endif
	#endregion

	#region 存在方法
	public bool Contains(object key) => key is not null && _cache.TryGetValue(key, out _);
	#endregion

	#region 压缩方法
	/// <summary>清理缓存。</summary>
	/// <param name="percentage">指定的清理百分比，介于<c>0</c>至<c>1</c>之间的浮点数。</param>
	/// <remarks>因为缓存不会实时清理内存，因此会存在某些过期或依赖项失效的缓存项仍然贮存在缓存中，可调用该方法触发清理动作。</remarks>
	public void Compact(double percentage = 0) => _cache.Compact(percentage);
	#endregion

	#region 删除方法
	#if NET7_0_OR_GREATER
	public void Clear() => _cache.Clear();
	#else
	public void Clear() => _cache.Compact(1.0);
	#endif

	public void Remove(object key) => _cache.Remove(key);
	public bool Remove(object key, out object value)
	{
		if(key is not null && _cache.TryGetValue(key, out value))
		{
			_cache.Remove(key);
			return true;
		}

		value = null;
		return false;
	}
	#endregion

	#region 获取方法
	public object GetValue(object key) => _cache.Get(key);
	public TValue GetValue<TValue>(object key) => _cache.Get<TValue>(key);

	public bool TryGetValue(object key, out object value) => _cache.TryGetValue(key, out value);
	public bool TryGetValue<TValue>(object key, out TValue value) => _cache.TryGetValue(key, out value);
	#endregion

	#region 获取设置
	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法将其返回的结果作为缓存项值进行缓存。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">当指定键的缓存项不存在时，构建缓存值的方法。</param>
	/// <returns>返回指定键的缓存值。</returns>
	/// <remarks>提示：当指定键对应的缓存项不存在，由于本方法<paramref name="factory"/>参数返回的结果并未包含过期时间，因此如果需要指定对应的过期时间或废除依赖，请调用同名方法的其他重载。</remarks>
	public TValue GetOrCreate<TValue>(object key, Func<TValue> factory)
	{
		var result = _cache.GetOrCreate(key, entry => factory == null ? default : factory.Invoke());

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, Expiration Expiration)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var expiration) = factory(entry.Key);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, Expiration Expiration)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var priority, var expiration) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, Expiration Expiration, object State)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var expiration, var state) = factory(entry.Key);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, Expiration Expiration, object State)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var priority, var expiration, var state) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, IChangeToken Dependency)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var dependency) = factory(entry.Key);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, IChangeToken Dependency)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var priority, var dependency) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, IChangeToken Dependency, object State)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var dependency, var state) = factory(entry.Key);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, IChangeToken Dependency, object State)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var priority, var dependency, var state) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, IChangeToken Dependency, Expiration Expiration)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var dependency, var expiration) = factory(entry.Key);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, IChangeToken Dependency, Expiration Expiration)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var priority, var dependency, var expiration) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, IChangeToken Dependency, Expiration Expiration, object State)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var dependency, var expiration, var state) = factory(entry.Key);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值。</returns>
	public TValue GetOrCreate<TValue>(object key, Func<object, (TValue Value, CachePriority Priority, IChangeToken Dependency, Expiration Expiration, object State)> factory)
	{
		var result = _cache.GetOrCreate(key, entry =>
		{
			(var value, var priority, var dependency, var expiration, var state) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法将其返回的结果作为缓存项值进行缓存。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">当指定键的缓存项不存在时，构建缓存值的异步方法。</param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	/// <remarks>提示：当指定键对应的缓存项不存在，由于本方法<paramref name="factory"/>参数返回的结果并未包含过期时间，因此如果需要指定对应的过期时间或废除依赖，请调用同名方法的其他重载。</remarks>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<Task<TValue>> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry => factory == null ? default : factory.Invoke());

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, Expiration Expiration)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var expiration) = factory(entry.Key);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, Expiration Expiration)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var priority, var expiration) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, Expiration Expiration, object State)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var expiration, var state) = factory(entry.Key);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, Expiration Expiration, object State)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var priority, var expiration, var state) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, IChangeToken Dependency)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var dependency) = factory(entry.Key);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, IChangeToken Dependency)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var priority, var dependency) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, IChangeToken Dependency, object State)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var dependency, var state) = factory(entry.Key);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, IChangeToken Dependency, object State)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var priority, var dependency, var state) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
			{
				entry.AddExpirationToken(dependency);
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);
			}

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, IChangeToken Dependency, Expiration Expiration)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var dependency, var expiration) = factory(entry.Key);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, IChangeToken Dependency, Expiration Expiration)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var priority, var dependency, var expiration) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, IChangeToken Dependency, Expiration Expiration, object State)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var dependency, var expiration, var state) = factory(entry.Key);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}

	/// <summary>获取指定键对应的缓存项，如果指定键的缓存项不存在则调用<paramref name="factory"/>参数的构建方法并根据其结果设置新建缓存项的值及相关设置。</summary>
	/// <typeparam name="TValue">泛型参数，表示缓存值的类型。</typeparam>
	/// <param name="key">指定的缓存项的键。</param>
	/// <param name="factory">
	///		当指定键的缓存项不存在时构建缓存项的异步方法，返回结果包括：
	///		<list type="bullet">
	///			<item>
	///				<term><c>Value</c></term>
	///				<description>缓存值的异步任务。</description>
	///			</item>
	///			<item>
	///				<term><c>Priority</c></term>
	///				<description>缓存项的优先级别。</description>
	///			</item>
	///			<item>
	///				<term><c>Dependency</c></term>
	///				<description>缓存项的废除依赖。</description>
	///			</item>
	///			<item>
	///				<term><c>Expiration</c></term>
	///				<description>缓存项的过期时间（支持滑动过期和绝对过期）。</description>
	///			</item>
	///			<item>
	///				<term><c>State</c></term>
	///				<description>缓存项 <see cref="Evicted"/> 废除事件的 <see cref="CacheEvictedEventArgs.State"/> 参数值。</description>
	///			</item>
	///		</list>
	/// </param>
	/// <returns>返回指定键的缓存值的异步任务。</returns>
	public async ValueTask<TValue> GetOrCreateAsync<TValue>(object key, Func<object, (Task<TValue> Value, CachePriority Priority, IChangeToken Dependency, Expiration Expiration, object State)> factory)
	{
		var result = await _cache.GetOrCreateAsync(key, entry =>
		{
			(var value, var priority, var dependency, var expiration, var state) = factory(entry.Key);
			entry.Priority = GetPriority(priority);

			if(dependency != null)
				entry.AddExpirationToken(dependency);
			if(expiration.IsSliding(out var sliding))
				entry.SlidingExpiration = sliding;
			if(expiration.IsAbsolute(out var absolute))
				entry.AbsoluteExpiration = absolute;

			if(dependency != null || expiration.HasValue)
				entry.RegisterPostEvictionCallback(this.OnEvicted, state);

			return value;
		});

		if(_options.IsLimit(out var limit) && _cache.Count > limit)
			this.OnLimited(_cache.Count - limit);

		return result;
	}
	#endregion

	#region 设置方法
	public void SetValue<TValue>(object key, TValue value, object state = null) => this.SetValue(key, value, CachePriority.Normal, state);
	public void SetValue<TValue>(object key, TValue value, CachePriority priority, object state = null)
	{
		using var entry = _cache.CreateEntry(key);
		entry.Value = value;
		entry.Priority = GetPriority(priority);

		if(state != null)
			entry.RegisterPostEvictionCallback(this.OnEvicted, state);

		if(_options.IsLimit(out var limit) && _cache.Count >= limit)
			this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
	}

	public void SetValue<TValue>(object key, TValue value, Expiration expiration, object state = null) => this.SetValue(key, value, CachePriority.Normal, expiration, state);
	public void SetValue<TValue>(object key, TValue value, CachePriority priority, Expiration expiration, object state = null)
	{
		using var entry = _cache.CreateEntry(key);
		entry.Value = value;
		entry.Priority = GetPriority(priority);

		if(expiration.IsSliding(out var sliding))
			entry.SlidingExpiration = sliding;
		if(expiration.IsAbsolute(out var absolute))
			entry.AbsoluteExpiration = absolute;

		if(expiration.HasValue)
			entry.RegisterPostEvictionCallback(this.OnEvicted, state);

		if(_options.IsLimit(out var limit) && _cache.Count >= limit)
			this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
	}

	public void SetValue<TValue>(object key, TValue value, IChangeToken dependency, object state = null) => this.SetValue(key, value, CachePriority.Normal, dependency, state);
	public void SetValue<TValue>(object key, TValue value, CachePriority priority, IChangeToken dependency, object state = null)
	{
		using var entry = _cache.CreateEntry(key);
		entry.Value = value;
		entry.Priority = GetPriority(priority);

		if(dependency != null)
		{
			entry.AddExpirationToken(dependency);
			entry.RegisterPostEvictionCallback(this.OnEvicted, state);
		}

		if(_options.IsLimit(out var limit) && _cache.Count >= limit)
			this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
	}

	public void SetValue<TValue>(object key, TValue value, IChangeToken dependency, Expiration expiration, object state = null) => this.SetValue(key, value, CachePriority.Normal, dependency, expiration, state);
	public void SetValue<TValue>(object key, TValue value, CachePriority priority, IChangeToken dependency, Expiration expiration, object state = null)
	{
		using var entry = _cache.CreateEntry(key);
		entry.Value = value;
		entry.Priority = GetPriority(priority);

		if(dependency != null)
			entry.AddExpirationToken(dependency);
		if(expiration.IsSliding(out var sliding))
			entry.SlidingExpiration = sliding;
		if(expiration.IsAbsolute(out var absolute))
			entry.AbsoluteExpiration = absolute;

		if(dependency != null || expiration.HasValue)
			entry.RegisterPostEvictionCallback(this.OnEvicted, state);

		if(_options.IsLimit(out var limit) && _cache.Count >= limit)
			this.OnLimited(_cache.Count - limit + 1, _cache.Count + 1);
	}
	#endregion

	#region 事件触发
	private void OnLimited(int limit, int count = 0) => this.OnLimited(new CacheLimitedEventArgs(limit, count > 0 ? count : _cache.Count));
	protected virtual void OnLimited(CacheLimitedEventArgs args) => this.Limited?.Invoke(this, args);

	private void OnEvicted(object key, object value, EvictionReason reason, object state) => this.OnEvicted(new CacheEvictedEventArgs(key, value, GetReason(reason), state));
	protected virtual void OnEvicted(CacheEvictedEventArgs args) => this.Evicted?.Invoke(this, args);
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static CacheItemPriority GetPriority(CachePriority priority) => priority switch
	{
		CachePriority.Normal => CacheItemPriority.Normal,
		CachePriority.Low => CacheItemPriority.Low,
		CachePriority.High => CacheItemPriority.High,
		CachePriority.NeverRemove => CacheItemPriority.NeverRemove,
		_ => CacheItemPriority.Normal,
	};

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static CacheEvictedReason GetReason(EvictionReason reason) => reason switch
	{
		EvictionReason.Removed => CacheEvictedReason.Removed,
		EvictionReason.Expired => CacheEvictedReason.Expired,
		EvictionReason.Capacity => CacheEvictedReason.Overfull,
		EvictionReason.Replaced => CacheEvictedReason.Replaced,
		EvictionReason.TokenExpired => CacheEvictedReason.Depended,
		_ => CacheEvictedReason.None,
	};
	#endregion

	#region 处置方法
	public void Dispose()
	{
		//确保单例的共享内存缓存实例不能被释放
		if(object.ReferenceEquals(this, Shared))
			return;

		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		var cache = Interlocked.Exchange(ref _cache, null);

		if(cache != null)
		{
			//移除“Limited”事件的所有处理函数
			var handlers = this.Limited.GetInvocationList();
			for(int i = 0; i < handlers.Length; i++)
				this.Limited -= (EventHandler<CacheLimitedEventArgs>)handlers[i];

			//移除“Evicted”事件的所有处理函数
			handlers = this.Evicted.GetInvocationList();
			for(int i = 0; i < handlers.Length; i++)
				this.Evicted -= (EventHandler<CacheEvictedEventArgs>)handlers[i];

			cache.Dispose();
		}
	}
	#endregion

	#region 嵌套结构
	public readonly struct Expiration : IEquatable<Expiration>
	{
		#region 私有字段
		private readonly TimeSpan       _sliding;
		private readonly DateTimeOffset _absolute;
		#endregion

		#region 构造函数
		public Expiration(TimeSpan value) : this(value, DateTimeOffset.MinValue) { }
		public Expiration(DateTimeOffset value) : this(value, TimeSpan.Zero) { }

		public Expiration(TimeSpan sliding, DateTimeOffset absolute)
		{
			_sliding = sliding;
			_absolute = absolute;
		}

		public Expiration(DateTimeOffset absolute, TimeSpan sliding)
		{
			_sliding = sliding;
			_absolute = absolute;
		}
		#endregion

		#region 公共属性
		public bool IsEmpty => _sliding <= TimeSpan.Zero && _absolute <= DateTimeOffset.MinValue;
		public bool HasValue => _sliding > TimeSpan.Zero || _absolute > DateTimeOffset.MinValue;
		#endregion

		#region 公共方法
		public bool IsSliding(out TimeSpan value)
		{
			value = _sliding;
			return value > TimeSpan.Zero;
		}

		public bool IsAbsolute(out DateTimeOffset value)
		{
			value = _absolute;
			return _absolute > DateTimeOffset.MinValue;
		}
		#endregion

		#region 重写方法
		public bool Equals(Expiration other) => this._sliding == other._sliding && this._absolute == other._absolute;
		public override bool Equals(object obj) => obj is Expiration other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(_sliding, _absolute);
		public override string ToString()
		{
			if(_sliding > TimeSpan.Zero)
			{
				if(_absolute > DateTimeOffset.MinValue)
					return $"Sliding: {_sliding}; Absolute: {_absolute}";
				else
					return $"Sliding: {_sliding}";
			}

			return _absolute > DateTimeOffset.MinValue ? $"Absolute: {_absolute}" : string.Empty;
		}
		#endregion

		#region 符号重写
		public static bool operator ==(Expiration left, Expiration right) => left.Equals(right);
		public static bool operator !=(Expiration left, Expiration right) => !(left == right);

		public static implicit operator Expiration(TimeSpan value) => new(value);
		public static implicit operator Expiration(DateTime value) => new(value);
		public static implicit operator Expiration(DateTimeOffset value) => new(value);
		public static implicit operator Expiration(ValueTuple<TimeSpan, DateTime> value) => new(value.Item1, value.Item2);
		public static implicit operator Expiration(ValueTuple<TimeSpan, DateTimeOffset> value) => new(value.Item1, value.Item2);
		public static implicit operator Expiration(ValueTuple<DateTime, TimeSpan> value) => new(value.Item1, value.Item2);
		public static implicit operator Expiration(ValueTuple<DateTimeOffset, TimeSpan> value) => new(value.Item1, value.Item2);
		#endregion
	}
	#endregion
}
