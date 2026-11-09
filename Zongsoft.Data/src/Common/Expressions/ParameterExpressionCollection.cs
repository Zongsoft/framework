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
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Common.Expressions;

public class ParameterExpressionCollection() : KeyedCollection<string, ParameterExpression>(StringComparer.OrdinalIgnoreCase)
{
	#region 私有变量
	private int _index;
	#endregion

	#region 公共方法
	public ParameterExpression Add(string name, DataType type, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input)
	{
		var parameter = Expression.Parameter(name, type, direction);
		this.Add(parameter);
		return parameter;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(ParameterExpression parameter) => parameter.Name;
	protected override void InsertItem(int index, ParameterExpression parameter)
	{
		if(parameter == null)
			throw new ArgumentNullException(nameof(parameter));

		//处理匿名参数，即参数名为空或问号(?)的参数
		if(string.IsNullOrEmpty(parameter.Name) || parameter.Name == ParameterExpression.ANONYMOUS)
			this.Anonymize(++_index, parameter);

		//调用基类同名方法
		base.InsertItem(index, parameter);
	}
	#endregion

	#region 虚拟方法
	/// <summary>处理匿名参数。</summary>
	/// <param name="index">匿名参数的序号，从 <c>1</c> 开始。</param>
	/// <param name="parameter">待处理的匿名参数对象。</param>
	protected virtual void Anonymize(int index, ParameterExpression parameter) => parameter.Name = $"p{index}";
	#endregion
}
