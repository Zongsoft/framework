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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Plugins
{
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
}
