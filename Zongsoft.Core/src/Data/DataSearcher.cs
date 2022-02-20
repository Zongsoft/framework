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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

namespace Zongsoft.Data
{
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
		public string Name { get => this.DataService.Name; }

		public IDataService<TModel> DataService { get; }

		public IDataSearcherConditioner Conditioner
		{
			get => _conditioner;
			set => _conditioner = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 计数方法
		public int Count(string keyword, string filter = null, IDataOptions options = null)
		{
			return this.OnCount(this.Resolve(nameof(Count), keyword, filter, options), options);
		}

		protected virtual int OnCount(ICondition criteria, IDataOptions options = null)
		{
			return this.DataService.Count(
				criteria,
				string.Empty,
				options as DataAggregateOptions);
		}
		#endregion

		#region 存在方法
		public bool Exists(string keyword, string filter = null, IDataOptions options = null)
		{
			return this.OnExists(this.Resolve(nameof(Exists), keyword, filter, options), options);
		}

		protected virtual bool OnExists(ICondition criteria, IDataOptions options = null)
		{
			return this.DataService.Exists(criteria, options as DataExistsOptions);
		}
		#endregion

		#region 搜索方法
		public IEnumerable<TModel> Search(string keyword, string filter = null, params Sorting[] sortings)
		{
			return this.Search(keyword, string.Empty, null, null, filter, sortings);
		}

		public IEnumerable<TModel> Search(string keyword, IDataOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Search(keyword, string.Empty, null, options, filter, sortings);
		}

		public IEnumerable<TModel> Search(string keyword, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Search(keyword, string.Empty, paging, null, filter, sortings);
		}

		public IEnumerable<TModel> Search(string keyword, string schema, string filter = null, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, null, null, filter, sortings);
		}

		public IEnumerable<TModel> Search(string keyword, string schema, IDataOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, null, options, filter, sortings);
		}

		public IEnumerable<TModel> Search(string keyword, string schema, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, paging, null, filter, sortings);
		}

		public IEnumerable<TModel> Search(string keyword, string schema, Paging paging, IDataOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.OnSearch(
				this.Resolve(nameof(Search), keyword, filter, options),
				schema,
				paging,
				options,
				sortings);
		}

		protected virtual IEnumerable<TModel> OnSearch(ICondition criteria, string schema, Paging paging, IDataOptions options, params Sorting[] sortings)
		{
			return this.DataService.Select(
				criteria,
				schema,
				paging,
				options as DataSelectOptions,
				sortings);
		}
		#endregion

		#region 条件解析
		protected virtual ICondition Resolve(string method, string keyword, string filter = null, IDataOptions options = null)
		{
			var conditioner = this.Conditioner;

			if(conditioner == null)
				throw new InvalidOperationException("Missing the required keyword condition resolver.");

			return conditioner.Resolve(method, keyword, filter, options);
		}
		#endregion

		#region 显式实现
		IEnumerable IDataSearcher.Search(string keyword, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, string.Empty, null, null, filter, sortings);
		}

		IEnumerable IDataSearcher.Search(string keyword, IDataOptions options, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, string.Empty, null, options, filter, sortings);
		}

		IEnumerable IDataSearcher.Search(string keyword, Paging paging, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, string.Empty, paging, null, filter, sortings);
		}

		IEnumerable IDataSearcher.Search(string keyword, string schema, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, null, null, filter, sortings);
		}

		IEnumerable IDataSearcher.Search(string keyword, string schema, IDataOptions options, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, null, options, filter, sortings);
		}

		IEnumerable IDataSearcher.Search(string keyword, string schema, Paging paging, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, paging, null, filter, sortings);
		}

		IEnumerable IDataSearcher.Search(string keyword, string schema, Paging paging, IDataOptions options, string filter, params Sorting[] sortings)
		{
			return this.Search(keyword, schema, paging, options, filter, sortings);
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
			public ICondition Resolve(string method, string keyword, string filter = null, IDataOptions options = null)
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
						throw new InvalidOperationException("Missing specified search member definitions.");

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

					var converter = TypeDescriptor.GetConverter(member);

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
	}
}
