/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据模型元信息的类。
	/// </summary>
	public class ModelDescriptor
	{
		#region 成员字段
		private string _title;
		private string _description;
		private readonly Type _type;
		private readonly IDataEntity _entity;
		#endregion

		#region 构造函数
		public ModelDescriptor(IDataEntity entity, Type type)
		{
			_entity = entity ?? throw new ArgumentNullException(nameof(entity));
			_type = type ?? throw new ArgumentNullException(nameof(type));
			this.Properties = new ModelPropertyDescriptorCollection(this);
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据实体定义。</summary>
		[System.Text.Json.Serialization.JsonIgnore]
		[Serialization.SerializationMember(Ignored = true)]
		public IDataEntity Entity => _entity;

		/// <summary>获取所属命名空间。</summary>
		public string Namespace => _entity.Namespace;

		/// <summary>获取模型名称。</summary>
		public string Name => _entity.Name;

		/// <summary>获取模型别名。</summary>
		public string Alias => _entity.Alias;

		/// <summary>获取模型类型。</summary>
		public Type Type => _type;

		/// <summary>获取模型属性元信息集。</summary>
		public ModelPropertyDescriptorCollection Properties { get; }

		/// <summary>获取或设置模型标题。</summary>
		public string Title
		{
			get => string.IsNullOrEmpty(_title) ? GetTitle() : _title;
			set => _title = value;
		}

		/// <summary>获取或设置模型描述文本。</summary>
		public string Description
		{
			get => string.IsNullOrEmpty(_description) ? GetDescription() : _description;
			set => _description = value;
		}
		#endregion

		#region 私有方法
		private string GetTitle() => Resources.ResourceUtility.GetResourceString(this.Type.Assembly, $"{this.Name}.{nameof(this.Title)}") ?? this.Name;
		private string GetDescription() => Resources.ResourceUtility.GetResourceString(this.Type.Assembly, $"{this.Name}.{nameof(this.Description)}");
		#endregion

		#region 重写方法
		public override string ToString() => string.IsNullOrEmpty(this.Namespace) ?
			$"{this.Name}@{Zongsoft.Common.TypeExtension.GetTypeAlias(this.Type)}" :
			$"{this.Namespace}:{this.Name}@{Zongsoft.Common.TypeExtension.GetTypeAlias(this.Type)}";
		#endregion
	}
}
