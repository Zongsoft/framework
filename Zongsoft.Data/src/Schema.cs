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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

/// <summary>表示由数据映射元数据绑定的模式。</summary>
public class Schema : Schema<SchemaMember>
{
	private readonly SchemaParser _parser;

	internal Schema(SchemaParser parser, string text, IDataEntity entity, Type modelType, IEnumerable<SchemaMember> entries) : base(entity?.Name, text, modelType, entries)
	{
		_parser = parser ?? throw new ArgumentNullException(nameof(parser));
		this.Entity = entity ?? throw new ArgumentNullException(nameof(entity));
	}

	public IDataEntity Entity { get; }

	protected override IEnumerable<SchemaMember> OnInclude(string expression) => _parser.Append(this, expression);
}
