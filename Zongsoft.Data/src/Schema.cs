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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public class Schema : ISchema, ISchema<SchemaMember>
	{
		#region 成员字段
		private SchemaParser _parser;
		private Collections.INamedCollection<SchemaMember> _members;
		#endregion

		#region 构造函数
		internal Schema(SchemaParser parser, string text, Metadata.IDataEntity entity, Type modelType, Collections.INamedCollection<SchemaMember> entries)
		{
			_parser = parser ?? throw new ArgumentNullException(nameof(parser));
			this.Text = text ?? throw new ArgumentNullException(nameof(text));
			this.Entity = entity ?? throw new ArgumentNullException(nameof(entity));
			this.ModelType = modelType;
			_members = entries ?? new Collections.NamedCollection<SchemaMember>(entry => entry.Name, StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public string Name { get => this.Entity.Name; }

		public string Text { get; }

		public Metadata.IDataEntity Entity { get; }

		public Type ModelType { get; }

		public bool IsEmpty { get => _members == null || _members.Count == 0; }

		public Collections.INamedCollection<SchemaMember> Members { get => _members; }
		#endregion

		#region 公共方法
		public void Clear()
		{
			if(_members != null)
				_members.Clear();
		}

		public bool Contains(string path)
		{
			if(string.IsNullOrEmpty(path) || this.IsEmpty)
				return false;

			var parts = path.Split('.', '/');
			var members = _members;

			for(int i = 0; i < parts.Length; i++)
			{
				if(members == null)
					return false;

				if(string.IsNullOrEmpty(parts[i]))
					continue;

				if(members.TryGet(parts[i], out var member))
					members = member.Children;
				else
					return false;
			}

			return true;
		}

		public SchemaMember Find(string path)
		{
			if(string.IsNullOrEmpty(path) || this.IsEmpty)
				return null;

			var parts = path.Split('.', '/');
			var members = _members;
			var member = (SchemaMember)null;

			for(int i = 0; i < parts.Length; i++)
			{
				if(members == null)
					return null;

				if(string.IsNullOrEmpty(parts[i]))
					continue;

				if(members.TryGet(parts[i], out member))
					members = member.Children;
				else
					return null;
			}

			return member;
		}

		public ISchema<SchemaMember> Include(string path)
		{
			if(string.IsNullOrEmpty(path))
				return this;

			var count = 0;
			var chars = new char[path.Length];

			for(int i = 0; i < chars.Length; i++)
			{
				if(path[i] == '.' || path[i] == '/')
				{
					chars[i] = '{';
					count++;
				}
				else
				{
					chars[i] = path[i];
				}
			}

			//由解析器统一进行解析处理
			_parser.Append(this, count == 0 ? path : new string(chars) + new string('}', count));

			return this;
		}

		public ISchema<SchemaMember> Exclude(string path)
		{
			this.Exclude(path, out _);
			return this;
		}

		public bool Exclude(string path, out SchemaMember member)
		{
			//设置输出参数默认值
			member = null;

			if(string.IsNullOrEmpty(path))
				return false;

			bool Remove(SchemaMember owner, string name, out SchemaMember removedMember)
			{
				var members = owner == null ? _members : (owner.HasChildren ? owner.Children : null);

				if(members != null && members.TryGet(name, out removedMember) && members.Remove(name))
				{
					if(owner != null && !owner.HasChildren)
						Remove(owner.Parent, owner.Name, out _);

					return true;
				}

				removedMember = null;
				return false;
			}

			int last = 0;
			SchemaMember current = null;

			for(int i = 0; i < path.Length; i++)
			{
				if(path[i] == '.' || path[i] == '/' && i > last)
				{
					var part = path.Substring(last, i - last);

					if(current == null)
					{
						if(!_members.TryGet(part, out current))
							return false;
					}
					else
					{
						if(current.HasChildren)
						{
							if(!_members.TryGet(part, out current))
								return false;
						}
						else
							return false;
					}

					last = i + 1;
				}
			}

			if(last < path.Length)
				return Remove(current, path.Substring(last), out member);

			return false;
		}
		#endregion

		#region 显式实现
		SchemaMemberBase ISchema.Find(string path)
		{
			return this.Find(path);
		}

		ISchema ISchema.Include(string path)
		{
			return this.Include(path);
		}

		ISchema ISchema.Exclude(string path)
		{
			this.Exclude(path, out _);
			return this;
		}

		bool ISchema.Exclude(string path, out SchemaMemberBase member)
		{
			member = null;

			if(this.Exclude(path, out var value))
			{
				member = value;
				return true;
			}

			return false;
		}
		#endregion
	}
}
