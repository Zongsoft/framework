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

	public static bool HasExited(this Process process, out int exitCode)
	{
		try
		{
			if(process == null)
			{
				exitCode = 0;
				return true;
			}

			exitCode = process.ExitCode;
			return process.HasExited;
		}
		catch { exitCode = 0; return false; }
	}

	public static string GetProcessInfo(this Process process)
	{
		if(process == null)
			return null;

		int exitCode;
		var info = GetInfo(process);

		if(info == null)
			return $"[{process.Id}]{GetName(process)}({(process.HasExited(out exitCode) ? $"Exited:{exitCode}" : "Running")})";

		var text = new System.Text.StringBuilder();
		text.AppendLine($"[{process.Id}]{GetName(process)}({(process.HasExited(out exitCode) ? $"Exited:{exitCode}" : "Running")})");
		text.AppendLine("{");

		text.AppendLine($"\t{nameof(info.FileName)}:{info.FileName}");
		text.AppendLine($"\t{nameof(info.WorkingDirectory)}:{info.WorkingDirectory}");

		if(!string.IsNullOrEmpty(info.Verb))
			text.AppendLine($"\t{nameof(info.Verb)}:{info.Verb}");

		if(info.Verbs != null && info.Verbs.Length > 0)
			text.AppendLine($"\t{nameof(info.Verbs)}:[{string.Join(',', info.Verbs)}]");

		if(!string.IsNullOrEmpty(info.Arguments))
			text.AppendLine($"\t{nameof(info.Arguments)}:{info.Arguments}");

		if(info.ArgumentList != null && info.ArgumentList.Count > 0)
			text.AppendLine($"\t{nameof(info.ArgumentList)}:[{string.Join(',', info.ArgumentList)}]");

		text.AppendLine("}");
		return text.ToString();

		static string GetName(Process process)
		{
			try { return process.ProcessName; }
			catch { return GetInfo(process).FileName; }
		}

		static ProcessStartInfo GetInfo(Process process)
		{
			try { return process.StartInfo; }
			catch { return null; }
		}
	}
}
