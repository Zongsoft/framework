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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示一个通用的硬件设备。
/// </summary>
public class Hardware : IHardware
{
	#region 构造函数
	/// <summary>初始化 <see cref="Hardware"/> 类的新实例。</summary>
	/// <param name="name">硬件名称。</param>
	/// <param name="code">硬件代码。</param>
	/// <param name="type">硬件类型。</param>
	/// <param name="category">硬件分类，多级分类以斜杠(<c>/</c>)分隔。</param>
	/// <param name="driver">硬件驱动程序。</param>
	/// <param name="properties">硬件属性集。</param>
	/// <param name="components">硬件组件集。</param>
	public Hardware(
		string name,
		string code,
		string type,
		string category,
		IHardwareDriver driver = null,
		IEnumerable<HardwareProperty> properties = null,
		IEnumerable<HardwareComponent> components = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Code = code;
		this.Type = type;
		this.Category = category;
		this.Driver = driver;
		this.Components = components == null ? new() : new(components);
		this.Properties = properties == null ? new() : new(properties);
	}

	/// <summary>初始化 <see cref="Hardware"/> 类的新实例。</summary>
	/// <param name="name">硬件名称。</param>
	/// <param name="code">硬件代码。</param>
	/// <param name="type">硬件类型。</param>
	/// <param name="model">硬件型号。</param>
	/// <param name="serie">硬件系列。</param>
	/// <param name="category">硬件分类，多级分类以斜杠(<c>/</c>)分隔。</param>
	/// <param name="driver">硬件驱动程序。</param>
	/// <param name="properties">硬件属性集。</param>
	/// <param name="components">硬件组件集。</param>
	public Hardware(
		string name,
		string code,
		string type,
		string model,
		string serie,
		string category,
		IHardwareDriver driver = null,
		IEnumerable<HardwareProperty> properties = null,
		IEnumerable<HardwareComponent> components = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Code = code;
		this.Type = type;
		this.Model = model;
		this.Serie = serie;
		this.Category = category;
		this.Driver = driver;
		this.Components = components == null ? new() : new(components);
		this.Properties = properties == null ? new() : new(properties);
	}

	/// <summary>初始化 <see cref="Hardware"/> 类的新实例。</summary>
	/// <param name="name">硬件名称。</param>
	/// <param name="code">硬件代码。</param>
	/// <param name="type">硬件类型。</param>
	/// <param name="model">硬件型号。</param>
	/// <param name="serie">硬件系列。</param>
	/// <param name="category">硬件分类，多级分类以斜杠(<c>/</c>)分隔。</param>
	/// <param name="manufacturer">生成厂商。</param>
	/// <param name="description">描述信息。</param>
	/// <param name="driver">硬件驱动程序。</param>
	/// <param name="properties">硬件属性集。</param>
	/// <param name="components">硬件组件集。</param>
	public Hardware(
		string name,
		string code,
		string type,
		string model,
		string serie,
		string category,
		string manufacturer,
		string description,
		IHardwareDriver driver = null,
		IEnumerable<HardwareProperty> properties = null,
		IEnumerable<HardwareComponent> components = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Code = code;
		this.Type = type;
		this.Model = model;
		this.Serie = serie;
		this.Category = category;
		this.Manufacturer = manufacturer;
		this.Description = description;
		this.Driver = driver;
		this.Components = components == null ? new() : new(components);
		this.Properties = properties == null ? new() : new(properties);
	}
	#endregion

	#region 公共属性
	public string Code { get; init; }
	public string Name { get; init; }
	public string Type { get; init; }
	public string Model { get; init; }
	public string Serie { get; init; }
	public string Category { get; init; }
	public string Manufacturer { get; init; }
	public string Description { get; init; }
	public IHardwareDriver Driver { get; init; }
	public HardwarePropertyCollection Properties { get; }
	public HardwareComponentCollection Components { get; }
	#endregion

	#region 公共方法
	public virtual bool HasUnique(out string identifier)
	{
		identifier = null;
		return false;
	}
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Code) ? this.Name : $"{this.Name}#{this.Code}";
	#endregion

	#region 静态方法
	public static Hardware Unique(
		string id,
		string name,
		string code,
		string type,
		string category,
		IHardwareDriver driver = null,
		IEnumerable<HardwareProperty> properties = null,
		IEnumerable<HardwareComponent> components = null) => new Uniqueness(id, name, code, type, category, driver, properties, components);

	public static Hardware Unique(
		string id,
		string name,
		string code,
		string type,
		string model,
		string serie,
		string category,
		IHardwareDriver driver = null,
		IEnumerable<HardwareProperty> properties = null,
		IEnumerable<HardwareComponent> components = null) => new Uniqueness(id, name, code, type, model, serie, category, driver, properties, components);

	public static Hardware Unique(
		string id,
		string name,
		string code,
		string type,
		string model,
		string serie,
		string category,
		string manufacturer,
		string description,
		IHardwareDriver driver = null,
		IEnumerable<HardwareProperty> properties = null,
		IEnumerable<HardwareComponent> components = null) => new Uniqueness(id, name, code, type, model, serie, category, manufacturer, description, driver, properties, components);
	#endregion

	#region 嵌套子类
	private sealed class Uniqueness : Hardware
	{
		public Uniqueness(
			string id,
			string name,
			string code,
			string type,
			string category,
			IHardwareDriver driver = null,
			IEnumerable<HardwareProperty> properties = null,
			IEnumerable<HardwareComponent> components = null) : base(name, code, type, category, driver, properties, components) => this.Identifier = id;

		public Uniqueness(
			string id,
			string name,
			string code,
			string type,
			string model,
			string serie,
			string category,
			IHardwareDriver driver = null,
			IEnumerable<HardwareProperty> properties = null,
			IEnumerable<HardwareComponent> components = null) : base(name, code, type, model, serie, category, driver, properties, components) => this.Identifier = id;

		public Uniqueness(
			string id,
			string name,
			string code,
			string type,
			string model,
			string serie,
			string category,
			string manufacturer,
			string description,
			IHardwareDriver driver = null,
			IEnumerable<HardwareProperty> properties = null,
			IEnumerable<HardwareComponent> components = null) : base(name, code, type, model, serie, category, manufacturer, description, driver, properties, components) => this.Identifier = id;

		public string Identifier
		{
			get;
			private init => field = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
		}

		public override bool HasUnique(out string identifier)
		{
			identifier = this.Identifier;
			return identifier != null;
		}

		public override string ToString()
		{
			if(string.IsNullOrEmpty(this.Identifier))
				return base.ToString();

			return string.IsNullOrEmpty(this.Code) ? $"({this.Identifier}){this.Name}" : $"({this.Identifier}){this.Name}#{this.Code}";
		}
	}
	#endregion
}
