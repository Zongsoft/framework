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

namespace Zongsoft.Data.Archiving;

/// <summary>表示数据模板提供程序的接口。</summary>
public interface IDataTemplateProvider
{
	/// <summary>获取指定名称及类型的数据模板。</summary>
	/// <param name="name">指定的模板名称。</param>
	/// <param name="format">指定的模板格式。</param>
	/// <returns>如果获取成功则返回对应的数据模板，否则返回空(<c>null</c>)。</returns>
	IDataTemplate GetTemplate(string name, string format = null);
}