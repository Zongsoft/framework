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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.ComponentModel;

namespace Zongsoft.Caching;

/// <summary>表示分布式缓存通知的订阅选项。</summary>
public class DistributedCacheSubscriptionOptions : INotifyPropertyChanged, INotifyPropertyChanging
{
	#region 事件声明
	public event PropertyChangedEventHandler PropertyChanged;
	public event PropertyChangingEventHandler PropertyChanging;
	#endregion

	#region 单例字段
	/// <summary>获取默认订阅选项。</summary>
	public static readonly DistributedCacheSubscriptionOptions Default = new ImmutableOptions();
	#endregion

	#region 成员字段
	private int _capacity;
	private string _prefix;
	private DistributedCacheNotificationKind _kind;
	private DistributedCacheNotificationOverflowPolicy _overflowPolicy;
	#endregion

	#region 构造函数
	/// <summary>初始化默认订阅选项。</summary>
	public DistributedCacheSubscriptionOptions()
	{
		_kind = DistributedCacheNotificationKind.All;
		_capacity = 1024;
	}

	/// <summary>初始化指定逻辑键前缀和通知种类的订阅选项。</summary>
	/// <param name="prefix">大小写敏感的逻辑缓存键前缀。</param>
	/// <param name="kind">要接收的通知种类组合。</param>
	public DistributedCacheSubscriptionOptions(string prefix, DistributedCacheNotificationKind kind = DistributedCacheNotificationKind.All)
		: this()
	{
		this.Prefix = prefix;
		this.Kind = kind;
	}

	/// <summary>使用指定的订阅选项创建一个经过验证的副本。</summary>
	/// <param name="options">要复制的订阅选项。</param>
	/// <exception cref="ArgumentNullException"><paramref name="options"/>为空。</exception>
	/// <exception cref="ArgumentOutOfRangeException"><see cref="Kind"/>为空或包含未定义的通知种类。</exception>
	public DistributedCacheSubscriptionOptions(DistributedCacheSubscriptionOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		this.Prefix = options.Prefix;
		this.Kind = options.Kind;
		this.Capacity = options.Capacity;
		this.OverflowPolicy = options.OverflowPolicy;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置单个订阅允许积压的最大通知数，默认为<c>1024</c>。</summary>
	public int Capacity
	{
		get => _capacity;
		set
		{
			if(value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, "The notification queue capacity must be positive.");

			if(_capacity == value)
				return;

			this.OnPropertyChanging(nameof(this.Capacity));
			_capacity = value;
			this.OnPropertyChanged(nameof(this.Capacity));
		}
	}

	/// <summary>获取或设置按<see cref="StringComparison.Ordinal"/>比较的大小写敏感逻辑缓存键前缀，空值或空白表示全部缓存键。</summary>
	public string Prefix
	{
		get => _prefix;
		set
		{
			var prefix = string.IsNullOrWhiteSpace(value) ? string.Empty : value;

			if(string.Equals(_prefix, prefix))
				return;

			this.OnPropertyChanging(nameof(this.Prefix));
			_prefix = prefix;
			this.OnPropertyChanged(nameof(this.Prefix));
		}
	}

	/// <summary>获取或设置要接收的通知种类组合；该值不能为<see cref="DistributedCacheNotificationKind.None"/>或包含未定义标志。</summary>
	public DistributedCacheNotificationKind Kind
	{
		get => _kind;
		set
		{
			if(!DistributedCacheNotification.IsValid(value))
				throw new ArgumentOutOfRangeException(nameof(value), value, "The notification kinds must contain only defined flags and cannot be None.");

			if(_kind == value)
				return;

			this.OnPropertyChanging(nameof(this.Kind));
			_kind = value;
			this.OnPropertyChanged(nameof(this.Kind));
		}
	}

	/// <summary>获取或设置通知队列溢出时的处理策略。</summary>
	public DistributedCacheNotificationOverflowPolicy OverflowPolicy
	{
		get => _overflowPolicy;
		set
		{
			if(!Enum.IsDefined(value))
				throw new ArgumentOutOfRangeException(nameof(value));

			if(_overflowPolicy == value)
				return;

			this.OnPropertyChanging(nameof(this.OverflowPolicy));
			_overflowPolicy = value;
			this.OnPropertyChanged(nameof(this.OverflowPolicy));
		}
	}
	#endregion

	#region 公共方法
	/// <summary>创建当前选项的不可变快照。</summary>
	/// <returns>返回不受当前实例后续修改影响的不可变选项。</returns>
	public DistributedCacheSubscriptionOptions Snapshot() => new ImmutableOptions(this);
	#endregion

	#region 保护方法
	protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	protected virtual void OnPropertyChanging(string propertyName) => this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
	#endregion

	#region 嵌套子类
	private sealed class ImmutableOptions : DistributedCacheSubscriptionOptions
	{
		public ImmutableOptions() { }
		public ImmutableOptions(DistributedCacheSubscriptionOptions options)
		{
			_prefix = options.Prefix;
			_kind = options.Kind;
			_capacity = options.Capacity;
			_overflowPolicy = options.OverflowPolicy;
		}

		protected override void OnPropertyChanged(string propertyName) => throw new NotSupportedException();
		protected override void OnPropertyChanging(string propertyName) => throw new NotSupportedException();
	}
	#endregion
}
