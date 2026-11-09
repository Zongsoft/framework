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

namespace Zongsoft.Terminals;

[Flags]
public enum TerminalStyles
{
	BackgroundColor = 1,
	ForegroundColor = 2,
	Color = BackgroundColor | ForegroundColor,

	FontName = 4,
	FontSize = 8,
	FontStyle = 16,
	FontWeight = 32,
	Font = FontName | FontSize | FontStyle | FontWeight,
}
