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

namespace Zongsoft.Data.Common.Expressions;

public class CommonTableExpression : Expression, ISource
{
	public CommonTableExpression(string name, Statement statement, params IEnumerable<string> columns) : this(name, false, statement, columns) { }
	public CommonTableExpression(string name, bool recursive, Statement statement, params IEnumerable<string> columns)
	{
		this.Name = name;
		this.Recursive = recursive;
		this.Statement = statement;
		this.Columns = [.. columns];
	}

	public string Name { get; }
	string ISource.Alias => null;
	public bool Recursive { get; }
	public ICollection<string> Columns { get; }
	public Statement Statement { get; set; }

	public FieldIdentifier CreateField(string name, string alias = null) => new(this, name, alias);
	public FieldIdentifier CreateField(IDataEntityProperty property) => new(this, property.GetFieldName(out var alias), alias)
	{
		Token = new DataEntityPropertyToken(property)
	};

	public override string ToString() => $"{this.Name}";
}
