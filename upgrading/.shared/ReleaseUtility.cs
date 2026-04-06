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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Upgrading;

public static class ReleaseUtility
{
	#region 私有常量
	private const string FILE_PATH = "__FILE_PATH__";
	#endregion

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

	public static void SetFilePath(this Release release, string path)
	{
		if(release == null)
			return;

		if(string.IsNullOrEmpty(path))
			release.Properties.Remove(FILE_PATH);
		else
			release.Properties[FILE_PATH] = path;
	}
}
