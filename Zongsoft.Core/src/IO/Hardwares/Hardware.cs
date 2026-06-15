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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示一个通用的硬件设备。
/// </summary>
public partial class Hardware : IHardware
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

	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
	public HardwarePropertyCollection Properties { get; }
	[JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
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

[JsonConverter(typeof(JsonConverter))]
partial class Hardware
{
	internal sealed class JsonConverter : JsonConverterFactory
	{
		public override bool CanConvert(Type type) => type == typeof(IHardware) || type == typeof(Hardware);

		public override System.Text.Json.Serialization.JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => type == typeof(Hardware) ?
			Converter<Hardware>.Default :
			Converter<IHardware>.Default;

		private sealed class Converter<T> : JsonConverter<T> where T : class, IHardware
		{
			const string Identifier = nameof(Identifier);
			public static readonly Converter<T> Default = new();

			public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
			{
				if(reader.TokenType == JsonTokenType.Null)
					return null;

				if(reader.TokenType != JsonTokenType.StartObject)
					throw new JsonException();

				string identifier = null;
				string name = null;
				string code = null;
				string typeName = null;
				string model = null;
				string serie = null;
				string category = null;
				string manufacturer = null;
				string description = null;
				HardwarePropertyCollection properties = null;
				HardwareComponentCollection components = null;

				while(reader.Read())
				{
					if(reader.TokenType == JsonTokenType.EndObject)
						break;

					if(reader.TokenType != JsonTokenType.PropertyName)
						throw new JsonException();

					var property = reader.GetString();

					if(!reader.Read())
						throw new JsonException();

					if(string.Equals(property, Identifier, StringComparison.OrdinalIgnoreCase))
						identifier = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Name), StringComparison.OrdinalIgnoreCase))
						name = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Code), StringComparison.OrdinalIgnoreCase))
						code = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Type), StringComparison.OrdinalIgnoreCase))
						typeName = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Model), StringComparison.OrdinalIgnoreCase))
						model = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Serie), StringComparison.OrdinalIgnoreCase))
						serie = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Category), StringComparison.OrdinalIgnoreCase))
						category = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Manufacturer), StringComparison.OrdinalIgnoreCase))
						manufacturer = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Description), StringComparison.OrdinalIgnoreCase))
						description = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
					else if(string.Equals(property, nameof(IHardware.Properties), StringComparison.OrdinalIgnoreCase))
						properties = JsonSerializer.Deserialize<HardwarePropertyCollection>(ref reader, options);
					else if(string.Equals(property, nameof(IHardware.Components), StringComparison.OrdinalIgnoreCase))
						components = JsonSerializer.Deserialize<HardwareComponentCollection>(ref reader, options);
					else
						reader.Skip();
				}

				if(string.IsNullOrEmpty(name))
					throw new JsonException($"The '{nameof(IHardware.Name)}' property is required.");

				var hardware = string.IsNullOrWhiteSpace(identifier) ?
					new Hardware(name, code, typeName, model, serie, category, manufacturer, description, null, properties, components) :
					Hardware.Unique(identifier, name, code, typeName, model, serie, category, manufacturer, description, null, properties, components);

				return (T)(object)hardware;
			}

			public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
			{
				if(value == null)
				{
					writer.WriteNullValue();
					return;
				}

				writer.WriteStartObject();

				if(value.HasUnique(out var identifier))
					writer.WriteString(HardwareUtility.GetName(options, Identifier), identifier);

				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Name)), value.Name);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Code)), value.Code);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Type)), value.Type);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Model)), value.Model);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Serie)), value.Serie);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Category)), value.Category);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Manufacturer)), value.Manufacturer);
				writer.WriteString(HardwareUtility.GetName(options, nameof(IHardware.Description)), value.Description);

				if(value.Properties != null && value.Properties.Count > 0)
				{
					writer.WritePropertyName(HardwareUtility.GetName(options, nameof(IHardware.Properties)));
					JsonSerializer.Serialize(writer, value.Properties, options);
				}

				if(value.Components != null && value.Components.Count > 0)
				{
					writer.WritePropertyName(HardwareUtility.GetName(options, nameof(IHardware.Components)));
					JsonSerializer.Serialize(writer, value.Components, options);
				}

				writer.WriteEndObject();
			}
		}
	}
}