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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示数据服务的注解类。</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
public class DataServiceAttribute : Attribute
{
	#region 构造函数
	public DataServiceAttribute(params string[] sortings) => this.Sortings = sortings;
	public DataServiceAttribute(Type criteria, params string[] sortings)
	{
		this.Criteria = criteria;
		this.Sortings = sortings;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置查询或过滤条件的实体类型。</summary>
	public Type Criteria { get; set; }

	/// <summary>获取或设置排序规则。</summary>
	public string[] Sortings { get; set; }

	/// <summary>获取一个值，指示是否包含排序规则。</summary>
	public bool HasSortings => this.Sortings != null && this.Sortings.Length > 0;
	#endregion

	#region 公共方法
	public Sorting[] GetSortings()
	{
		var sortings = this.Sortings;

		if(sortings == null || sortings.Length == 0)
			return null;

		var result = new HashSet<Sorting>(sortings.Length);

		for(int i = 0; i < sortings.Length; i++)
		{
			if(Sorting.TryParse(sortings[i], out var sorting))
				result.Add(sorting);
		}

		return result.Count > 0 ? result.ToArray() : null;
	}
	#endregion
}

/// <summary>表示数据服务的注解类。</summary>
/// <typeparam name="TCriteria">数据服务的查询或过滤条件的实体类型。</typeparam>
/// <remarks>构造一个数据服务注解。</remarks>
/// <param name="sortings">指定的默认排序规则。</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
public class DataServiceAttribute<TCriteria>(params string[] sortings) : DataServiceAttribute(typeof(TCriteria), sortings) where TCriteria : class, IModel
{
}
