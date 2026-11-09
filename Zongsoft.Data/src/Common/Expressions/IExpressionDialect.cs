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

namespace Zongsoft.Data.Common.Expressions;

public interface IExpressionDialect
{
	string Name { get; }
	string GetDataType(DataType type, int length, byte precision, byte scale);
	string GetSymbol(Operator @operator);
	string GetIdentifier(string name);
	string GetIdentifier(ReturningKind kind);
	string GetIdentifier(IIdentifier identifier);
	string GetAlias(string alias);
	string GetMethodName(MethodExpression method);
}
