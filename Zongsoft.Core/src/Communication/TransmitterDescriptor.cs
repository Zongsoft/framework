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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Communication
{
	public class TransmitterDescriptor : IEquatable<TransmitterDescriptor>
	{
		#region 构造函数
		public TransmitterDescriptor(string name, string title = null, string description = null)
		{
			this.Name = name ?? throw new ArgumentNullException(nameof(name));
			this.Title = title;
			this.Description = description;
			this.Channels = new ChannelDescriptorCollection();
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Title { get; set; }
		public string Description { get; set; }
		public ChannelDescriptorCollection Channels { get; }
		#endregion

		#region 重写方法
		public bool Equals(TransmitterDescriptor other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is TransmitterDescriptor other && this.Equals(other);
		public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
		public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
		#endregion

		#region 嵌套子类
		public class ChannelDescriptor : IEquatable<ChannelDescriptor>
		{
			#region 构造函数
			public ChannelDescriptor(string name, string title = null, string description = null)
			{
				this.Name = name ?? throw new ArgumentNullException(nameof(name));
				this.Title = title;
				this.Description = description;
				this.Templates = new TemplateCollection();
			}
			#endregion

			#region 公共属性
			public string Name { get; }
			public string Title { get; set; }
			public string Description { get; set; }
			public TemplateCollection Templates { get; }
			#endregion

			#region 重写方法
			public bool Equals(ChannelDescriptor other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
			public override bool Equals(object obj) => obj is ChannelDescriptor other && this.Equals(other);
			public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
			public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
			#endregion

			#region 嵌套子类
			public sealed class Template : IEquatable<Template>
			{
				#region 构造函数
				public Template(string name, string title = null, string description = null)
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
				public bool Equals(Template other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
				public override bool Equals(object obj) => obj is Template other && this.Equals(other);
				public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
				public override string ToString() => string.IsNullOrEmpty(this.Title) ? this.Name : $"[{this.Name}]{this.Title}";
				#endregion
			}

			public class TemplateCollection() : KeyedCollection<string, Template>(StringComparer.OrdinalIgnoreCase)
			{
				protected override string GetKeyForItem(Template template) => template.Name;
			}
			#endregion
		}

		public class ChannelDescriptorCollection() : KeyedCollection<string, ChannelDescriptor>(StringComparer.OrdinalIgnoreCase)
		{
			protected override string GetKeyForItem(ChannelDescriptor channel) => channel.Name;
		}
		#endregion
	}
}
