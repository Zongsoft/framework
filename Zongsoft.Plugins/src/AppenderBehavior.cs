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

public enum AppenderBehavior
{
	/// <summary>追加</summary>
	[AppenderDescription(AppenderBehavior.Appending)]
	Appending,

	/// <summary>替换</summary>
	[AppenderDescription(AppenderBehavior.Replacement)]
	Replacement,
}

internal sealed class AppenderDescriptionAttribute(AppenderBehavior behavior) : DescriptionAttribute
{
	public override string Description => behavior == AppenderBehavior.Appending ?
		Properties.Resources.AppenderBehavior_Appending_Description :
		Properties.Resources.AppenderBehavior_Replacement_Description;
}
