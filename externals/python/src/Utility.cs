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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Python library.
 *
 * The Zongsoft.Externals.Python is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Python is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Python library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace Zongsoft.Externals.Python;

internal static class Utility
{
	const string RESULT_VARIABLE = "result";

	public static object GetResult(this ScriptEngine engine) => engine.Runtime.Globals.TryGetVariable(RESULT_VARIABLE, out var result) ? result : null;
	public static object GetResult(this ScriptScope scope) => scope.TryGetVariable(RESULT_VARIABLE, out var result) ? result : null;
}
