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
using System.ComponentModel;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据分页的设置类。
/// </summary>
[TypeConverter(typeof(PagingConverter))]
public partial class Paging : INotifyPropertyChanged, INotifyPropertyChanging
{
	#region 常量定义
	private const int PAGE_SIZE = 20;
	#endregion

	#region 事件声明
	public event PropertyChangedEventHandler PropertyChanged;
	public event PropertyChangingEventHandler PropertyChanging;
	#endregion

	#region 静态字段
	public static readonly Paging Disabled = new ImmutablePaging();
	#endregion

	#region 成员字段
	private int _size;
	private int _index;
	private long _total;
	private long _offset;
	#endregion

	#region 构造函数
	/// <summary>创建默认的分页设置。<see cref="Index"/>默认值为<c>1</c>（即首页），<see cref="Size"/>默认值为<c>20</c>。</summary>
	public Paging() : this(1, PAGE_SIZE) { }

	/// <summary>创建指定页号的分页设置。<see cref="Size"/>默认值为<c>20</c>。</summary>
	/// <param name="index">指定的页号（从<c>1</c>开始）。</param>
	public Paging(int index) : this(index, PAGE_SIZE) { }

	/// <summary>创建指定页号和页大小的分页设置。</summary>
	/// <param name="index">指定的页号（从<c>1</c>开始）。</param>
	/// <param name="size">指定的页大小，如果为零则表示不分页。</param>
	public Paging(int index, int size)
	{
		_total = -1;
		_size = Math.Max(size, 0);
		_index = Math.Max(index, 0);
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示分页结果是否为空集。</summary>
	/// <remarks>注意：只有当 <see cref="Total"/> 等于零，本属性才会返回真(<c>True</c>)。</remarks>
	public bool IsEmpty => _total == 0;

	/// <summary>获取或设置页大小，如果该属性值为零则表示不分页。</summary>
	public int Size
	{
		get => _size;
		set
		{
			this.OnPropertyChanging(nameof(this.Size));
			_size = Math.Max(value, 0);
			this.OnPropertyChanged(nameof(this.Size));
		}
	}

	/// <summary>获取或设置当前查询的页号（从<c>1</c>开始），如果页号为零则表示不分页。</summary>
	/// <remarks>注意：零表示不分页，仅获取 <see cref="Size"/> 属性所指定的记录数。</remarks>
	public int Index
	{
		get => _index;
		set
		{
			this.OnPropertyChanging(nameof(this.Index));
			_index = Math.Max(value, 0);
			this.OnPropertyChanged(nameof(this.Index));
		}
	}

	/// <summary>获取查询结果的总页数。</summary>
	public int Count
	{
		get
		{
			if(_total < 1)
				return 0;

			if(_size < 1)
				return 1;

			return (int)Math.Ceiling((double)_total / _size);
		}
	}

	/// <summary>获取或设置查询结果的总记录数。</summary>
	/// <remarks>如果返回值小于零，则表示尚未进行分页操作。</remarks>
	public long Total
	{
		get => _total;
		set
		{
			this.OnPropertyChanging(nameof(this.Total));
			_total = Math.Max(value, -1);
			this.OnPropertyChanged(nameof(this.Total));
		}
	}
	#endregion

	#region 公共方法
	/// <summary>判断是否为分页模式。</summary>
	/// <returns>如果返回真(<c>True</c>)则表示为分页模式。</returns>
	public bool IsPaged() => _size > 0 && _index > 0;

	/// <summary>判断是否为分页模式。</summary>
	/// <param name="index">输出参数，表示分页的页号。</param>
	/// <param name="size">输出参数，表示分页的页大小。</param>
	/// <returns>如果返回真(<c>True</c>)则表示为分页模式。</returns>
	public bool IsPaged(out int index, out int size)
	{
		size = _size;
		index = _index;
		return size > 0 && index > 0;
	}

	/// <summary>判断是否为限制模式。</summary>
	/// <returns>如果返回真(<c>True</c>)则表示为限制模式。</returns>
	public bool IsLimited() => _size > 0 && _index == 0;

	/// <summary>判断是否为限制模式。</summary>
	/// <param name="count">输出参数，表示限制的记录数。</param>
	/// <param name="offset">输出参数，表示限制的偏移量。</param>
	/// <returns>如果返回真(<c>True</c>)则表示为限制模式。</returns>
	public bool IsLimited(out int count, out long offset)
	{
		count = _size;
		offset = _offset;
		return count > 0 && _index == 0;
	}
	#endregion

	#region 事件触发
	protected void OnPropertyChanged(string name) => this.OnPropertyChanged(new PropertyChangedEventArgs(name));
	protected virtual void OnPropertyChanged(PropertyChangedEventArgs args) => this.PropertyChanged?.Invoke(this, args);
	protected void OnPropertyChanging(string name) => this.OnPropertyChanging(new PropertyChangingEventArgs(name));
	protected virtual void OnPropertyChanging(PropertyChangingEventArgs args) => this.PropertyChanging?.Invoke(this, args);
	#endregion

	#region 重写方法
	public bool Equals(Paging other) => other is not null &&
		_size == other._size &&
		_index == other._index &&
		_total == other._total &&
		_offset == other._offset;

	public override bool Equals(object obj) => this.Equals(obj as Paging);
	public override int GetHashCode() => HashCode.Combine(_size, _index, _total, _offset);
	public override string ToString()
	{
		if(_total > 0)
			return $"{_index}/{this.Count}({_total})";

		if(_size < 1)
			return $"{_index}/{this.Count}";
		else
			return $"{_index}/{this.Count}[{_size}]";
	}
	#endregion

	#region 静态方法
	/// <summary>创建指定记录数的限定设置。</summary>
	/// <param name="count">限定返回的记录数，不能小于<c>1</c>。</param>
	/// <param name="offset">限定获取记录的偏移量，默认值为<c>0</c>。</param>
	/// <returns>返回新创建的分页设置对象。</returns>
	public static Paging Limit(int count = 1, long offset = 0) => new(0, Math.Max(count, 1)) { _offset = Math.Max(offset, 0) };

	/// <summary>创建指定页大小的首页设置。</summary>
	/// <param name="size">指定的页大小，不能小于<c>1</c>。</param>
	/// <returns></returns>
	public static Paging First(int size = PAGE_SIZE) => new(1, Math.Max(size, 1));

	/// <summary>以指定的页号及大小创建一个分页设置对象。</summary>
	/// <param name="index">指定的页号，默认为<c>1</c>。</param>
	/// <param name="size">每页的大小，默认为<c>20</c>。</param>
	/// <returns>返回新创建的分页设置对象。</returns>
	public static Paging Page(int index = 1, int size = PAGE_SIZE) => size > 0 ? new(index, size) : Disabled;
	#endregion

	#region 嵌套子类
	[ImmutableObject(true)]
	private sealed class ImmutablePaging() : Paging(0, 0)
	{
		protected override void OnPropertyChanged(PropertyChangedEventArgs args) => throw new InvalidOperationException();
		protected override void OnPropertyChanging(PropertyChangingEventArgs args) => throw new InvalidOperationException();
	}
	#endregion
}
