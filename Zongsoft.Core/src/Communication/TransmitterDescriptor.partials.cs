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
using System.Collections.ObjectModel;

namespace Zongsoft.Communication;

partial class TransmitterDescriptor
{
	public class Channel : IEquatable<Channel>
	{
		#region 构造函数
		public Channel(string name, string title = null, string description = null)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Title = title;
			this.Description = description;
			this.Templates = new();
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Title { get; set; }
		public string Description { get; set; }
		public TemplateCollection Templates { get; }
		#endregion

		#region 重写方法
		public bool Equals(Channel other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is Channel other && this.Equals(other);
		public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
		public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
		#endregion
	}

	public class ChannelCollection() : KeyedCollection<string, Channel>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(Channel channel) => channel.Name;
	}

	public class Template : IEquatable<Template>
	{
		#region 构造函数
		public Template(string name, string title = null, string description = null)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Title = title;
			this.Description = description;
			this.Parameters = new();
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Title { get; set; }
		public string Description { get; set; }
		public ParameterCollection Parameters { get; }
		#endregion

		#region 重写方法
		public bool Equals(Template other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is Template other && this.Equals(other);
		public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
		public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
		#endregion

		#region 嵌套子类
		public sealed class Parameter : IEquatable<Parameter>
		{
			#region 构造函数
			public Parameter(string name, string title = null, string description = null)
			{
				this.Name = name ?? throw new ArgumentNullException(nameof(name));
				this.Title = title;
				this.Description = description;
			}
			#endregion

			#region 公共属性
			public string Name { get; }
			public string Title { get; set; }
			public string Description { get; set; }
			#endregion

			#region 重写方法
			public bool Equals(Parameter other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
			public override bool Equals(object obj) => obj is Parameter other && this.Equals(other);
			public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
			public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
			#endregion
		}

		public class ParameterCollection() : KeyedCollection<string, Parameter>(StringComparer.OrdinalIgnoreCase)
		{
			protected override string GetKeyForItem(Parameter parameter) => parameter.Name;
		}
		#endregion
	}

	public class TemplateCollection() : KeyedCollection<string, Template>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(Template template) => template.Name;
	}
}
