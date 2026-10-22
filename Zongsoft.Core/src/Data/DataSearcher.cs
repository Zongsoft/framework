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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using Zongsoft.Collections;

namespace Zongsoft.Data;

public class DataSearcher<TModel> : IDataSearcher<TModel>, IDataSearcher
{
	#region 成员字段
	private IDataSearcherConditioner _conditioner;
	#endregion

	#region 构造函数
	public DataSearcher(IDataService<TModel> dataService)
	{
		this.DataService = dataService ?? throw new ArgumentNullException(nameof(dataService));

		var attributes = dataService.GetType().GetCustomAttributes<DataSearcherAttribute>(true).ToArray();
		if(attributes != null && attributes.Length > 0)
			this.Conditioner = DataSearcherResolver.Create(typeof(TModel).GetTypeInfo(), attributes);
	}
	#endregion

	#region 公共属性
	public string Name => this.DataService.Name;
	public IDataService<TModel> DataService { get; }
	public IDataSearcherConditioner Conditioner
	{
		get => _conditioner;
		set => _conditioner = value ?? throw new ArgumentNullException();
	}
	#endregion

	#region 条件解析
	protected virtual ICondition Resolve(string method, string keyword, IDataOptions options = null)
	{
		var conditioner = this.Conditioner;

		if(conditioner == null)
			throw new InvalidOperationException(Properties.Resources.DataSearcher_MissingResolver_Message);

		return conditioner.Resolve(method, keyword, options);
	}
	#endregion

	#region 嵌套子类
	private class DataSearcherResolver : IDataSearcherConditioner
	{
		#region 私有变量
		private readonly IDictionary<string, ConditionToken> _mapping;
		#endregion

		#region 构造函数
		private DataSearcherResolver(IDictionary<string, ConditionToken> mapping)
		{
			_mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
		}
		#endregion

		#region 解析方法
		public ICondition Resolve(string method, string keyword, IDataOptions options = null)
		{
			if(string.IsNullOrWhiteSpace(keyword))
				return null;

			var index = keyword.IndexOf(':');

			if(!_mapping.TryGetValue(index < 0 ? string.Empty : keyword.Substring(0, index).Trim(), out var token))
				return null;

			if(token.Members.Length == 1)
				return GetCondition(token.Members[0], keyword.Substring(index + 1).Trim());

			var conditions = new ConditionCollection(token.Combination);

			foreach(var member in token.Members)
			{
				conditions.Add(GetCondition(member, keyword.Substring(index + 1).Trim()));
			}

			return conditions;

			static Condition GetCondition(ConditionMemberToken member, string literal)
			{
				return member.IsExactly ?
					Condition.Equal(member.Name, Common.Convert.ConvertValue(literal, member.Type, () => member.Converter)) :
					Condition.Like(member.Name, literal + "%");
			}
		}
		#endregion

		#region 静态方法
		public static DataSearcherResolver Create(TypeInfo type, DataSearcherAttribute[] attributes)
		{
			if(attributes == null || attributes.Length == 0)
				return null;

			var mapping = new Dictionary<string, ConditionToken>(StringComparer.OrdinalIgnoreCase);

			foreach(var attribute in attributes)
			{
				foreach(var pattern in attribute.Patterns)
				{
					var index = pattern.IndexOf(':');

					if(index > 0)
					{
						var keys = pattern.Substring(0, index).Split(',');
						var token = ConditionToken.Create(type, pattern.Substring(index + 1).Split(','));

						if(keys.Length == 1)
						{
							mapping[keys[0].Trim()] = token;
						}
						else
						{
							foreach(var key in keys)
							{
								mapping[key.Trim()] = token;
							}
						}
					}
					else
					{
						var token = ConditionToken.Create(type, pattern.Split(','));

						if(token.Members.Length == 1)
							mapping[token.Members[0].Name] = token;
						else
						{
							mapping[string.Empty] = token;

							foreach(var field in token.Members)
							{
								mapping[field.Name] = token;
							}
						}
					}
				}
			}

			return new DataSearcherResolver(mapping);
		}
		#endregion

		#region 内部结构
		private struct ConditionToken
		{
			#region 公共字段
			/// <summary>条件组合方式</summary>
			public ConditionCombination Combination;

			/// <summary>条件成员数组</summary>
			public ConditionMemberToken[] Members;
			#endregion

			#region 构造函数
			public ConditionToken(ConditionMemberToken[] members)
			{
				this.Members = members;
				this.Combination = ConditionCombination.Or;
			}
			#endregion

			#region 静态方法
			public static ConditionToken Create(TypeInfo type, string[] members)
			{
				if(members == null || members.Length == 0)
					throw new ArgumentNullException(nameof(members));

				var tokens = new List<ConditionMemberToken>(members.Length);

				foreach(var member in members)
				{
					if(TryGetMember(type, member, out var path, out var memberInfo, out var isExactly))
						tokens.Add(new ConditionMemberToken(path, memberInfo, isExactly));
				}

				if(tokens.Count == 0)
					throw new InvalidOperationException(Properties.Resources.DataSearcher_MissingMembers_Message);

				return new ConditionToken(tokens.ToArray());
			}

			private static bool TryGetMember(TypeInfo type, string pattern, out string path, out MemberInfo member, out bool isExactly)
			{
				path = null;
				member = null;
				isExactly = true;

				if(string.IsNullOrEmpty(pattern))
					return false;

				isExactly = pattern[0] == '!' || pattern[pattern.Length - 1] == '!' || (pattern[0] != '?' && pattern[pattern.Length - 1] != '?');

				if(type == null || type.IsPrimitive || type == typeof(object).GetTypeInfo())
					return false;

				path = isExactly ? pattern.Trim('!') : pattern.Trim('?');
				var parts = path.Split('.');
				var inherits = new Stack<Type>();

				for(int i = 0; i < parts.Length; i++)
				{
					inherits.Clear();

					do
					{
						member = type.DeclaredMembers.FirstOrDefault(m => string.Equals(m.Name, parts[i], StringComparison.OrdinalIgnoreCase));

						if(member != null)
							break;

						if(type.BaseType != null)
							inherits.Push(type.BaseType);

						if(type.ImplementedInterfaces != null)
						{
							foreach(var contract in type.ImplementedInterfaces)
								inherits.Push(contract);
						}

						type = inherits.TryPop(out var inheritance) ? inheritance.GetTypeInfo() : null;
					} while(type != null && type != typeof(object).GetTypeInfo());

					if(member == null)
						return false;

					type = member.MemberType switch
					{
						MemberTypes.Field => ((FieldInfo)member).FieldType.GetTypeInfo(),
						MemberTypes.Property => ((PropertyInfo)member).PropertyType.GetTypeInfo(),
						MemberTypes.Method => ((MethodInfo)member).ReturnType.GetTypeInfo(),
						_ => null,
					};

					if(type == null)
						return false;
				}

				return member != null;
			}
			#endregion

			#region 重写方法
			public override string ToString()
			{
				var text = new System.Text.StringBuilder();

				for(int i = 0; i < this.Members.Length; i++)
				{
					if(i > 0)
						text.Append(" " + this.Combination.ToString() + " ");

					text.Append(this.Members[i].ToString());
				}

				return text.ToString();
			}
			#endregion
		}

		private readonly struct ConditionMemberToken
		{
			#region 公共字段
			/// <summary>成员名称</summary>
			public readonly string Name;

			/// <summary>成员类型</summary>
			public readonly Type Type;

			/// <summary>是否精确匹配</summary>
			public readonly bool IsExactly;

			/// <summary>类型转换器。</summary>
			public readonly TypeConverter Converter;
			#endregion

			#region 构造函数
			public ConditionMemberToken(string name, MemberInfo member, bool isExactly)
			{
				if(member == null)
					throw new ArgumentNullException(nameof(member));

				this.Name = name;
				this.Type = member switch { PropertyInfo property => property.PropertyType, FieldInfo field => field.FieldType, _ => null };
				this.IsExactly = isExactly;

				var converter = Common.Convert.GetTypeConverter(member);

				if(converter.GetType() != typeof(TypeConverter))
					this.Converter = converter;
				else
					this.Converter = null;
			}
			#endregion

			#region 重写方法
			public override string ToString()
			{
				if(this.IsExactly)
					return this.Name;
				else
					return this.Name + "?";
			}
			#endregion
		}
		#endregion
	}
	#endregion

	#region 异步方法
	public ValueTask<int> CountAsync(string keyword, IDataOptions options = null, CancellationToken cancellation = default) => this.OnCountAsync(this.Resolve("Count", keyword, options), options, cancellation);
	protected virtual ValueTask<int> OnCountAsync(ICondition criteria, IDataOptions options = null, CancellationToken cancellation = default) => this.DataService.CountAsync(criteria, options as DataAggregateOptions, cancellation);

	public ValueTask<bool> ExistsAsync(string keyword, IDataOptions options = null, CancellationToken cancellation = default) => this.OnExistsAsync(this.Resolve("Exists", keyword, options), options, cancellation);
	protected virtual ValueTask<bool> OnExistsAsync(ICondition criteria, IDataOptions options = null, CancellationToken cancellation = default) => this.DataService.ExistsAsync(criteria, options as DataExistsOptions, cancellation);

	public IAsyncEnumerable<TModel> SearchAsync(string keyword, params Sorting[] sortings) => this.SearchAsync(keyword, string.Empty, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, Sorting[] sortings, CancellationToken cancellation = default) => this.SearchAsync(keyword, string.Empty, null, null, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, params Sorting[] sortings) => this.SearchAsync(keyword, string.Empty, null, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, string.Empty, null, null, sortings, cancellation).Cast(cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, IDataOptions options, params Sorting[] sortings) => this.SearchAsync(keyword, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SearchAsync(keyword, string.Empty, null, options, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, IDataOptions options, params Sorting[] sortings) => this.SearchAsync(keyword, string.Empty, null, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, IDataOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, string.Empty, null, options, sortings, cancellation).Cast(cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, Paging paging, params Sorting[] sortings) => this.SearchAsync(keyword, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SearchAsync(keyword, string.Empty, paging, null, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, Paging paging, params Sorting[] sortings) => this.SearchAsync(keyword, string.Empty, paging, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, string.Empty, paging, null, sortings, cancellation).Cast(cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, params Sorting[] sortings) => this.SearchAsync(keyword, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SearchAsync(keyword, schema, null, null, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, params Sorting[] sortings) => this.SearchAsync(keyword, schema, null, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, schema, null, null, sortings, cancellation).Cast(cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, IDataOptions options, params Sorting[] sortings) => this.SearchAsync(keyword, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SearchAsync(keyword, schema, null, options, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, IDataOptions options, params Sorting[] sortings) => this.SearchAsync(keyword, schema, null, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, IDataOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, schema, null, options, sortings, cancellation).Cast(cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, params Sorting[] sortings) => this.SearchAsync(keyword, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SearchAsync(keyword, schema, paging, null, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, Paging paging, params Sorting[] sortings) => this.SearchAsync(keyword, schema, paging, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, null, sortings, cancellation).Cast(cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, params Sorting[] sortings) => this.SearchAsync(keyword, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.OnSearchAsync(this.Resolve("Search", keyword, options), schema, paging, options, sortings, cancellation);
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, params Sorting[] sortings) => this.SearchAsync(keyword, schema, paging, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataSearcher.SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, options, sortings, cancellation).Cast(cancellation);

	protected virtual IAsyncEnumerable<TModel> OnSearchAsync(ICondition criteria, string schema, Paging paging, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.DataService.SelectAsync(criteria, schema, paging, options as DataSelectOptions, sortings, cancellation);
	#endregion

	#region 便利取消
	public ValueTask<int> CountAsync(string keyword, CancellationToken cancellation) => this.CountAsync(keyword, default(IDataOptions), cancellation);
	public ValueTask<bool> ExistsAsync(string keyword, CancellationToken cancellation) => this.ExistsAsync(keyword, default(IDataOptions), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, CancellationToken cancellation) => this.SearchAsync(keyword, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, CancellationToken cancellation) => this.SearchAsync(keyword, schema, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, options, default(Sorting[]), cancellation);
	#endregion
}
