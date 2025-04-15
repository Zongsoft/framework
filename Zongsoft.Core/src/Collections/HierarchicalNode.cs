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
 * Copyright (C) 2020-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Collections;

/// <summary>
/// 表示层次结构的节点类。
/// </summary>
public abstract class HierarchicalNode : IHierarchicalNode, INotifyPropertyChanged
{
	#region 事件定义
	public event PropertyChangedEventHandler PropertyChanged;
	#endregion

	#region 常量定义
	public const char PathSeparator = '/';
	#endregion

	#region 静态字段
	public static readonly char[] IllegalCharacters = ['/', '\\', '*', '?', '!', '@', '#', '$', '%', '^', '&'];
	#endregion

	#region 成员字段
	private int? _hashCode;
	private string _path;
	private readonly string _name;
	#endregion

	#region 构造函数
	protected HierarchicalNode() => _name = PathSeparator.ToString();
	protected HierarchicalNode(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		if(name.IndexOfAny(IllegalCharacters) >= 0)
			throw new ArgumentException("The name contains illegal character(s).");

		_name = name.Trim();
	}
	#endregion

	#region 公共属性
	/// <summary>获取层次结构节点的名称，名称不可为空或空字符串，根节点的名称为斜杠(即“/”)。</summary>
	public string Name => _name;

	/// <summary>获取层次结构节点的路径。</summary>
	/// <remarks>如果为根节点则返回空字符串(<c>String.Empty</c>)，否则即为父节点的全路径。</remarks>
	public string Path => _path ?? this.GetPath();

	/// <summary>获取层次结构节点的完整路径，即节点路径与名称的组合。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public string FullPath
	{
		get
		{
			var path = this.Path;

			if(string.IsNullOrEmpty(path))
				return _name;

			if(path.Length == 1 && path[0] == PathSeparator)
				return path + _name;
			else
				return path + PathSeparator + _name;
		}
	}
	#endregion

	#region 抽象方法
	protected abstract string GetPath();
	#endregion

	#region 事件触发
	protected virtual void OnPropertyChanged(string name)
	{
		if(string.Equals(name, nameof(this.Path)) || string.Equals(name, nameof(this.FullPath)))
			_path = null;

		this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
	#endregion

	#region 重写方法
	public bool Equals(IHierarchicalNode other) => other is not null && string.Equals(this.FullPath, other.FullPath, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is HierarchicalNode other && this.Equals(other);
	public override int GetHashCode() => _hashCode ??= this.FullPath.ToUpperInvariant().GetHashCode();
	public override string ToString() => this.FullPath;
	#endregion
}
