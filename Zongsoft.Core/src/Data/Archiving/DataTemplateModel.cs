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
using System.Collections.Generic;

namespace Zongsoft.Data.Archiving;

public class DataTemplateModel : IDataTemplateModel
{
	#region 构造函数
	public DataTemplateModel(object data, IEnumerable<KeyValuePair<string, object>> parameters = null)
	{
		this.Data = data;
		this.Parameters = parameters == null ?
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) :
			new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	public object Data { get; }
	public IDictionary<string, object> Parameters { get; }
	#endregion
}