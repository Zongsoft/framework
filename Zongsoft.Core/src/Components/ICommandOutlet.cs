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
using System.IO;
using System.Text;

namespace Zongsoft.Components;

public interface ICommandOutlet
{
	Encoding Encoding { get; set; }
	TextWriter Writer { get; }

	void Write(CommandOutletContent content);
	void Write<T>(T value);
	void Write<T>(CommandOutletColor foregroundColor, T value);
	void Write<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);
	void Write<T>(CommandOutletStyles style, T value);
	void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value);
	void Write<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);

	void WriteLine();
	void WriteLine(CommandOutletContent content);
	void WriteLine<T>(T value);
	void WriteLine<T>(CommandOutletColor foregroundColor, T value);
	void WriteLine<T>(CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);
	void WriteLine<T>(CommandOutletStyles style, T value);
	void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, T value);
	void WriteLine<T>(CommandOutletStyles style, CommandOutletColor foregroundColor, CommandOutletColor backgroundColor, T value);
}
