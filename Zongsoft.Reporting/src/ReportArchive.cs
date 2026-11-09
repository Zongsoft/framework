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

namespace Zongsoft.Reporting;

public struct ReportArchive
{
	#region 构造函数
	public ReportArchive(string key, string name, string type, string path = null)
	{
		this.Key = key;
		this.Name = name;
		this.Type = type;
		this.Path = path;
	}
	#endregion

	#region 公共属性
	public string Key { get; }
	public string Name { get; }
	public string Type { get; }
	public string Path { get; }
	#endregion
}
