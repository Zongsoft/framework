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

namespace Zongsoft.Data.Metadata.Profiles;

/// <summary>表示数据命令的元数据类。</summary>
public partial class MetadataCommand : DataCommandBase<MetadataCommand.MetadataCommandScriptor>
{
	#region 构造函数
	public MetadataCommand(string @namespace, string name, string alias = null) : base(@namespace, name, alias)
	{
		this.Scriptor = new MetadataCommandScriptor(this);
	}
	#endregion
}
