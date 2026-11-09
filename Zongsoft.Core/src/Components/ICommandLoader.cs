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

namespace Zongsoft.Components;

/// <summary>提供命令加载的功能。</summary>
/// <remarks>对使用者的提醒：命令加载器不能重复使用，即不要把一个<see cref="ICommandLoader"/>实例赋予不同的用例，因为<seealso cref="IsLoaded"/>属性与不同的用例是无关的。</remarks>
public interface ICommandLoader
{
	/// <summary>获取一个值表示当前加载器是否已经加载完成。</summary>
	bool IsLoaded { get; }

	/// <summary>将命令加载到指定的命令树节点中。</summary>
	/// <param name="node">要挂载的命令树节点。</param>
	/// <remarks>对实现者的提醒：应该确保该方法的实现是线程安全的。</remarks>
	void Load(CommandNode node);
}
