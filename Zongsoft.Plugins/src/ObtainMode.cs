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
using System.ComponentModel;

namespace Zongsoft.Plugins;

/// <summary>表示当获取构件或者插件树节点中内部属性时的方式。</summary>
public enum ObtainMode
{
	/// <summary>复用已有实例；尚未完成时继续装配，尚无实例时创建并装配。</summary>
	Auto = 0,

	/// <summary>重新创建并更新节点缓存，不向所有者追加新实例。</summary>
	/// <remarks>保留原有所有者集合及其引用，不执行替换或释放旧实例。</remarks>
	Alway,

	/// <summary>无论Value属性是否为空，始终返回节点的Value。</summary>
	Never,
}
