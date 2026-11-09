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

namespace Zongsoft.Intelligences;

public class Model : IModel
{
	#region 构造函数
	public Model() { }
	public Model(string identifier, string name, long size, DateTimeOffset creation, string description = null) : this(identifier, name, size, null, creation, description) { }
	public Model(string identifier, string name, long size, string version, DateTimeOffset creation, string description = null)
	{
		this.Identifier = identifier;
		this.Name = name;
		this.Size = size;
		this.Version = version;
		this.Creation = creation;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public string Identifier { get; set; }
	public string Name { get; set; }
	public long Size { get; set; }
	public string Version { get; set; }
	public DateTimeOffset Creation { get; set; }
	public string Description { get; set; }
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}@{this.Creation}";
	#endregion
}
