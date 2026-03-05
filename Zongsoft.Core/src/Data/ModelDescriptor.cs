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

using Zongsoft.Common;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据模型元信息的类。
/// </summary>
public class ModelDescriptor : INotifyPropertyChanged, INotifyPropertyChanging
{
	#region 事件定义
	public event PropertyChangedEventHandler PropertyChanged;
	public event PropertyChangingEventHandler PropertyChanging;
	#endregion

	#region 构造函数
	public ModelDescriptor() => this.Properties = new(this);
	#endregion

	#region 公共属性
	/// <summary>获取或设置所属命名空间。</summary>
	public string Namespace
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Namespace));
			field = value;
			this.OnPropertyChanged(nameof(this.Namespace));
		}
	}

	/// <summary>获取或设置模型名称。</summary>
	public string Name
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Name));
			field = value;
			this.OnPropertyChanged(nameof(this.Name));
		}
	}

	/// <summary>获取或设置限定名称。</summary>
	public string QualifiedName
	{
		get => string.IsNullOrEmpty(this.Namespace) ? this.Name : $"{this.Namespace}{Type.Delimiter}{this.Name}";
		set
		{
			this.OnPropertyChanging(nameof(this.QualifiedName));
			if(string.IsNullOrEmpty(value))
			{
				this.Name = string.Empty;
				this.Namespace = null;
			}
			else
			{
				var index = value.LastIndexOf(Type.Delimiter);
				if(index < 0)
				{
					this.Name = value;
					this.Namespace = null;
				}
				else
				{
					this.Name = value[(index + 1)..];
					this.Namespace = value[..index];
				}
			}
			this.OnPropertyChanged(nameof(this.QualifiedName));
		}
	}

	/// <summary>获取或设置模型别名。</summary>
	public string Alias
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Alias));
			field = value;
			this.OnPropertyChanged(nameof(this.Alias));
		}
	}

	/// <summary>获取或设置模型类型。</summary>
	public Type Type
	{
		get; set
		{
			this.OnPropertyChanging(nameof(this.Type));
			field = value;
			this.OnPropertyChanged(nameof(this.Type));
		}
	}

	/// <summary>获取或设置模型标题。</summary>
	public string Title
	{
		get => string.IsNullOrEmpty(field) ? this.GetTitle() : field;
		set
		{
			this.OnPropertyChanging(nameof(this.Title));
			field = value;
			this.OnPropertyChanged(nameof(this.Title));
		}
	}

	/// <summary>获取或设置模型描述文本。</summary>
	public string Description
	{
		get => string.IsNullOrEmpty(field) ? this.GetDescription() : field;
		set
		{
			this.OnPropertyChanging(nameof(this.Description));
			field = value;
			this.OnPropertyChanged(nameof(this.Description));
		}
	}

	/// <summary>获取模型属性信息集。</summary>
	public ModelPropertyDescriptorCollection Properties { get; }
	#endregion

	#region 事件触发
	protected virtual void OnPropertyChanged(string propertyName) => this.PropertyChanged?.Invoke(this, new(propertyName));
	protected virtual void OnPropertyChanging(string propertyName) => this.PropertyChanging?.Invoke(this, new(propertyName));
	#endregion

	#region 私有方法
	private string GetTitle() => this.GetResourceString(
		$"{this.QualifiedName}.{nameof(this.Title)}",
		this.QualifiedName,
		$"{this.QualifiedName}.{nameof(this.Title)}",
		this.Name);

	private string GetDescription() => this.GetResourceString(
		$"{this.QualifiedName}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(this.Description)}");

	internal string GetResourceString(params ReadOnlySpan<string> names)
	{
		var type = this.Type;
		if(type != null)
			return Resources.ResourceUtility.GetResourceString(type, names);

		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		for(int i = 0; i < assemblies.Length; i++)
		{
			var module = Services.ApplicationModuleAttribute.Find(assemblies[i]);

			if(module != null && Equals(module.Name, this.Namespace))
			{
				var result = Resources.ResourceUtility.GetResourceString(assemblies[i], names);

				if(!string.IsNullOrEmpty(result))
					return result;
			}
		}

		return null;

		static bool Equals(string a, string b, StringComparison comparison = StringComparison.OrdinalIgnoreCase) =>
			(string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) || string.Equals(a, b, comparison);
	}
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(this.Type == null)
			return string.IsNullOrEmpty(this.Alias) ?
				$"{this.QualifiedName}" :
				$"{this.QualifiedName}({this.Alias})";
		else
			return string.IsNullOrEmpty(this.Alias) ?
				$"{this.QualifiedName}@{TypeAlias.GetAlias(this.Type)}" :
				$"{this.QualifiedName}@{TypeAlias.GetAlias(this.Type)}({this.Alias})";
	}
	#endregion
}
