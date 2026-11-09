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

namespace Zongsoft.Plugins;

public class PluginPath
{
	#region 静态方法
	public static string Combine(params string[] paths) => Zongsoft.IO.Path.Combine(paths);
	#endregion

	#region 内部方法
	internal static string PreparePathText(string text) => PreparePathText(text, out _);
	internal static string PreparePathText(string text, out ObtainMode mode)
	{
		mode = ObtainMode.Auto;

		if(string.IsNullOrEmpty(text))
			return string.Empty;

		var index = text.LastIndexOf(',');

		if(index < 0)
			return text;

		if(index < text.Length - 1)
			Enum.TryParse<ObtainMode>(text[(index + 1)..], true, out mode);

		return text[..index];
	}
	#endregion
}
