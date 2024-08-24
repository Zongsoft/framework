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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 提供数据命令内容管理的接口。
	/// </summary>
	public interface IDataCommandScriptor
	{
		/// <summary>
		/// 获取指定驱动的命令脚本内容。
		/// </summary>
		/// <param name="driver">指定要获取的脚本对应的驱动标识名。</param>
		/// <returns>返回脚本内容文本，如果指定的驱动没有对应的脚本则返回空(null)。</returns>
		string GetScript(string driver);

		/// <summary>
		/// 设置指定驱动的命令脚本内容。
		/// </summary>
		/// <param name="driver">指定要设置的脚本对应的驱动标识名。</param>
		/// <param name="text">要设置的脚本内容文本。</param>
		void SetScript(string driver, string text);
	}
}
