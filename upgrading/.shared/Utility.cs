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
using System.Diagnostics;

namespace Zongsoft.Upgrading;

public static class Utility
{
	/// <summary>判断指定的版本号是否为零。</summary>
	/// <param name="version">指定的版本。</param>
	/// <returns>如果版本号为零则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool IsZero(this Version version) => version == null ||
	(
		version.Major == 0 &&
		version.Minor == 0 &&
		version.Build == 0 &&
		version.Revision == 0
	);

	public static bool HasExited(this Process process)
	{
		try { return process == null || process.HasExited; }
		catch { return false; }
	}

	public static string GetProcessInfo(this Process process)
	{
		if(process == null)
			return null;

		if(process.StartInfo == null)
			return $"[{process.Id}]{process.ProcessName}({(process.HasExited() ? "Exited" : "Running")})";

		var text = new System.Text.StringBuilder();
		text.AppendLine($"[{process.Id}]{process.ProcessName}({(process.HasExited() ? "Exited" : "Running")})");
		text.AppendLine("{");

		text.AppendLine($"\t{nameof(process.StartInfo.FileName)}:{process.StartInfo.FileName}");
		text.AppendLine($"\t{nameof(process.StartInfo.WorkingDirectory)}:{process.StartInfo.WorkingDirectory}");

		if(!string.IsNullOrEmpty(process.StartInfo.Verb))
			text.AppendLine($"\t{nameof(process.StartInfo.Verb)}:{process.StartInfo.Verb}");

		if(process.StartInfo.Verbs != null && process.StartInfo.Verbs.Length > 0)
			text.AppendLine($"\t{nameof(process.StartInfo.Verbs)}:[{string.Join(',', process.StartInfo.Verbs)}]");

		if(!string.IsNullOrEmpty(process.StartInfo.Arguments))
			text.AppendLine($"\t{nameof(process.StartInfo.Arguments)}:{process.StartInfo.Arguments}");

		if(process.StartInfo.ArgumentList != null && process.StartInfo.ArgumentList.Count > 0)
			text.AppendLine($"\t{nameof(process.StartInfo.ArgumentList)}:[{string.Join(',', process.StartInfo.ArgumentList)}]");

		text.AppendLine("}");
		return text.ToString();
	}
}
