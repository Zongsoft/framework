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
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zongsoft.Web.Binders;

public class RangeModelBinder<T> : IModelBinder where T : struct, IComparable<T>
{
	#region 委托定义
	private delegate bool TryParseDelegate(string text, out Data.Range<T> value);
	#endregion

	#region 私有变量
	private readonly TryParseDelegate _TryParse_;
	#endregion

	#region 构造函数
	public RangeModelBinder()
	{
		var method = typeof(Data.Range<T>).GetMethod(nameof(Data.Range<T>.TryParse));
		_TryParse_ = (TryParseDelegate)method.CreateDelegate(typeof(TryParseDelegate));
	}
	#endregion

	#region 公共方法
	public Task BindModelAsync(ModelBindingContext context)
	{
		var value = context.ValueProvider.GetValue(context.ModelName);

		if(string.IsNullOrEmpty(value.FirstValue))
			context.Result = ModelBindingResult.Success(Zongsoft.Data.Range.Empty<T>());
		else
		{
			if(_TryParse_.Invoke(value.FirstValue, out var range))
				context.Result = ModelBindingResult.Success(range);
			else
				context.ModelState.TryAddModelError(context.ModelName, $"The specified '{context.ModelName}' parameter value '{value.FirstValue}' cannot be converted to the Range<{typeof(T).FullName}> type.");
		}

		return Task.CompletedTask;
	}
	#endregion
}
