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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public sealed class TDengineParameters : ParameterExpressionCollection
{
	protected override void Anonymize(int index, ParameterExpression parameter)
	{
		/*
		 * 注意：TDengine 对参数名的首字符有不同的含义：
		 *    $ 打头表示标签字段对应的参数；
		 *    @ 打头表示数据字段对应的参数。
		 */
		if(parameter.Field.Token.Property.IsTagField())
			parameter.Name = $"${index}";
		else
			parameter.Name = $"@{index}";
	}
}
