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

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示硬件及其组件的标识信息。
/// </summary>
public interface IHardwareIdentifier
{
	/// <summary>获取标识代码。</summary>
	string Code { get; }

	/// <summary>获取标识名称。</summary>
	string Name { get; }

	/// <summary>获取标识类型。</summary>
	string Type { get; }

	/// <summary>获取标识描述。</summary>
	string Description { get; }

	/// <summary>获取一个值，指示当前对象是否具有唯一编号。</summary>
	/// <param name="identifier">返回当前对象的唯一编号。</param>
	/// <returns>如果当前对象具有唯一编号则返回真(<c>true</c>)，否则返回假(<c>false</c>)。</returns>
	bool HasUnique(out string identifier);
}
