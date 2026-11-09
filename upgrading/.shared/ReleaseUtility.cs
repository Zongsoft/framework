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

namespace Zongsoft.Upgrading;

public static class ReleaseUtility
{
	#region 私有常量
	private const string FILE_PATH = "__FILE_PATH__";
	#endregion

	#region 公共方法
	public static bool TryGetFilePath(this Release release, out string result)
	{
		if(release != null && release.Properties.TryGetValue(FILE_PATH, out var value) && value is string text)
		{
			result = text;
			return !string.IsNullOrEmpty(text);
		}

		result = null;
		return false;
	}

	public static string GetFilePath(this Release release) => release != null && release.Properties.TryGetValue(FILE_PATH, out var value) ? value as string : null;
	public static void SetFilePath(this Release release, string path)
	{
		if(release == null)
			return;

		if(string.IsNullOrEmpty(path))
			release.Properties.Remove(FILE_PATH);
		else
			release.Properties[FILE_PATH] = path;
	}
	#endregion
}
