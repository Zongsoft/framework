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
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示特定类型的数据模式。
	/// </summary>
	/// <typeparam name="T">特定类型的泛型参数。</typeparam>
	public abstract class Schema<T> : ISchemaMemberProvider
	{
		#region 成员字段
		private readonly SchemaMemberCollection _members;
		#endregion

		#region 构造函数
		protected Schema()
		{
			_members = new SchemaMemberCollection();
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 清空当前模式的成员。
		/// </summary>
		/// <returns>返回当前模式。</returns>
		public Schema<T> Clear()
		{
			_members.Clear();
			return this;
		}

		/// <summary>
		/// 将指定 Lambda 表达式中的成员访问式添加到当前模式成员中。
		/// </summary>
		/// <typeparam name="TMember">泛型参数，表示成员访问表达式类型。</typeparam>
		/// <param name="expression">指定的包含成员访问的 Lambda 表达式。</param>
		/// <returns>返回当前模式。</returns>
		/// <example>
		/// <code>
		/// Schema.Empty&lt;Apartment&gt;()
		///		.Include(p => p.ApartmentId)
		///		.Include(p => p.ApartmentNo)
		///		.Include(p => p.Building.BuildingId)
		///		.Include(p => p.Building.BuildingNo)
		///		.Include(p => p.Building.ParkId)
		///		.Include(p => p.Building.Park.ParkId)
		///		.Include(p => p.Building.Park.ParkNo)
		///		.Include(p => p.Building.Park.Name)
		/// </code>
		/// </example>
		public Schema<T> Include<TMember>(Expression<Func<T, TMember>> expression)
		{
			ISchemaMember parent = null;
			var members = GetMembers(expression);

			foreach(var member in members)
			{
				var children = parent == null ? _members : parent.Children;

				if(!children.TryGetValue(member.Name, out var child))
				{
					child = this.GetMember(member.Name, parent);

					if(child == null)
						throw new DataArgumentException(member.Name);

					children.Add(child);
				}

				parent = child;
			}

			return this;
		}

		/// <summary>
		/// 将指定 Lambda 表达式中的成员访问式从当前模式成员中移除。
		/// </summary>
		/// <typeparam name="TMember">泛型参数，表示成员访问表达式类型。</typeparam>
		/// <param name="expression">指定的包含成员访问的 Lambda 表达式。</param>
		/// <returns>返回当前模式。</returns>
		/// <example>
		/// <code>
		/// Schema.Empty&lt;Apartment&gt;()
		///		.Exclude(p => p.ApartmentId)
		///		.Exclude(p => p.ApartmentNo)
		///		.Exclude(p => p.Building.BuildingId)
		///		.Exclude(p => p.Building.BuildingNo)
		///		.Exclude(p => p.Building.Park)
		/// </code>
		/// </example>
		public Schema<T> Exclude<TMember>(Expression<Func<T, TMember>> expression)
		{
			var members = GetMembers(expression);
			ISchemaMember parent = null, child = null;

			foreach(var member in members)
			{
				if(child != null)
					parent = child;

				if(parent == null)
				{
					if(!_members.TryGetValue(member.Name, out child))
						return this;
				}
				else if(!parent.HasChildren || !parent.Children.TryGetValue(member.Name, out child))
					return this;
			}

			if(child != null)
			{
				if(parent == null)
					_members.Remove(child);
				else if(parent.HasChildren)
					parent.Children.Remove(child);
			}

			return this;
		}
		#endregion

		#region 抽象方法
		protected abstract ISchemaMember GetMember(string name, ISchemaMember parent);
		#endregion

		#region 重写方法
		public override string ToString()
		{
			if(_members == null || _members.Count == 0)
				return string.Empty;

			var text = new System.Text.StringBuilder();

			foreach(var member in _members)
			{
				if(text.Length > 0)
					text.Append(',');

				WriteMember(text, member);
			}

			return text.ToString();
		}
		#endregion

		#region 私有方法
		private static IEnumerable<MemberInfo> GetMembers(Expression expression)
		{
			switch(expression.NodeType)
			{
				case ExpressionType.MemberAccess:
					yield return ((MemberExpression)expression).Member;

					if(((MemberExpression)expression).Expression != null)
					{
						foreach(var member in GetMembers(((MemberExpression)expression).Expression))
							yield return member;
					}

					break;
				case ExpressionType.Lambda:
					foreach(var member in GetMembers(((LambdaExpression)expression).Body))
						yield return member;

					break;
			}
		}

		private static void WriteMember(System.Text.StringBuilder text, ISchemaMember member)
		{
			text.Append(member.Name);

			if(member.Paging != null && member.Paging.Enabled)
			{
				text.Append(':');
				text.Append(member.Paging.PageIndex.ToString());
				text.Append('/');
				text.Append(member.Paging.PageSize.ToString());
			}

			if(member.Sortings != null && member.Sortings.Length > 0)
			{
				text.Append('(');

				for(int i = 0; i < member.Sortings.Length; i++)
				{
					if(i > 0)
						text.Append(',');

					text.Append(member.Sortings[i].ToString());
				}

				text.Append(')');
			}

			if(member.Criteria != null)
			{
				text.Append('?');
				text.Append(member.Criteria.ToString());
			}

			if(member.HasChildren)
			{
				text.Append('{');
				WriteMembers(text, member.Children);
				text.Append('}');
			}
		}

		private static void WriteMembers(System.Text.StringBuilder text, IEnumerable<ISchemaMember> members)
		{
			var index = 0;

			foreach(var member in members)
			{
				if(index++ > 0)
					text.Append(',');

				WriteMember(text, member);
			}
		}
		#endregion

		#region 显式接口
		ISchemaMember ISchemaMemberProvider.GetMember(string name, ISchemaMember parent) => this.GetMember(name, parent);
		#endregion
	}

	public class SchemaMemberCollection : KeyedCollection<string, ISchemaMember>
	{
		public SchemaMemberCollection() : base(StringComparer.OrdinalIgnoreCase) { }
		protected override string GetKeyForItem(ISchemaMember member) => member.Name;
	}

	public interface ISchemaMember
	{
		string Name { get; }
		string Path { get; }
		string FullPath { get; }
		ISchemaMember Parent { get; }
		MemberInfo Member { get; }
		Metadata.IDataEntityProperty Property { get; }

		Paging Paging { get; }
		Sorting[] Sortings { get; }
		ICondition Criteria { get; }

		bool HasChildren { get; }
		SchemaMemberCollection Children { get; }
	}

	public interface ISchemaMemberProvider
	{
		ISchemaMember GetMember(string name, ISchemaMember parent);
	}
}
