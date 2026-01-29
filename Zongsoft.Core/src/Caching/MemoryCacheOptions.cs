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
using System.ComponentModel;

namespace Zongsoft.Caching;

public class MemoryCacheOptions : INotifyPropertyChanged, INotifyPropertyChanging
{
	#region 常量定义
	private const int FREQUENCY_SECONDS = 60;
	#endregion

	#region 事件定义
	public event PropertyChangedEventHandler PropertyChanged;
	public event PropertyChangingEventHandler PropertyChanging;
	#endregion

	#region 成员字段
	private int _limit;
	private TimeSpan _frequency;
	#endregion

	#region 构造函数
	public MemoryCacheOptions(int limit = 0) : this(TimeSpan.FromSeconds(FREQUENCY_SECONDS), limit) { }
	public MemoryCacheOptions(TimeSpan frequency, int limit = 0)
	{
		_frequency = EnsureScanFrequency(frequency);
		_limit = EnsureCountLimit(limit);
	}
	#endregion

	#region 公共属性
	public int CountLimit
	{
		get => _limit;
		set
		{
			this.OnPropertyChanging(nameof(this.CountLimit));
			_limit = EnsureCountLimit(value);
			this.OnPropertyChanged(nameof(this.CountLimit));
		}
	}

	public TimeSpan ScanFrequency
	{
		get => _frequency;
		set
		{
			this.OnPropertyChanging(nameof(this.ScanFrequency));
			_frequency = EnsureScanFrequency(value);
			this.OnPropertyChanged(nameof(this.ScanFrequency));
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	protected virtual void OnPropertyChanging(string name) => this.PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(name));
	#endregion

	#region 公共方法
	public static MemoryCacheOptions Immutable(int limit = 0) => new ImmutableOptions(TimeSpan.FromSeconds(FREQUENCY_SECONDS), limit);
	public static MemoryCacheOptions Immutable(TimeSpan frequency, int limit = 0) => new ImmutableOptions(frequency, limit);
	#endregion

	#region 内部方法
	internal bool IsLimit(out int limit)
	{
		limit = _limit;
		return limit > 0;
	}
	#endregion

	#region 重写方法
	public override string ToString() => $"{nameof(this.CountLimit)}:{this.CountLimit}; {nameof(this.ScanFrequency)}:{this.ScanFrequency}";
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static int EnsureCountLimit(int limit) => limit > 0 ? limit : 0;

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static TimeSpan EnsureScanFrequency(TimeSpan frequency) => frequency.Ticks > TimeSpan.TicksPerSecond ? frequency : TimeSpan.FromSeconds(1);
	#endregion

	#region 嵌套子类
	private sealed class ImmutableOptions(TimeSpan frequency, int limit = 0) : MemoryCacheOptions(frequency, limit)
	{
		const string ERROR_MESSAGE = "The current memory cache options is immutable.";

		public override string ToString() => $"[Immutability] {base.ToString()}";
		protected override void OnPropertyChanged(string name) => throw new InvalidOperationException(ERROR_MESSAGE);
		protected override void OnPropertyChanging(string name) => throw new InvalidOperationException(ERROR_MESSAGE);
	}
	#endregion
}
