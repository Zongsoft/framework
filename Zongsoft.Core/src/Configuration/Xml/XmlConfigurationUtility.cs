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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Xml;

namespace Zongsoft.Configuration.Xml;

internal static class XmlConfigurationUtility
{
	internal static StringComparer PathComparer { get; } = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	internal static bool IsInstructionName(string name)
	{
		if(string.IsNullOrEmpty(name) || string.Equals(name, "xml", StringComparison.OrdinalIgnoreCase))
			return false;

		try
		{
			XmlConvert.VerifyName(name);
			return true;
		}
		catch(XmlException)
		{
			return false;
		}
	}

	internal static string NormalizeOptionPath(string path) => string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim('/', ' ', '\t').Replace('/', ':');

	internal static string GetFileIdentity(string path)
	{
		path = Path.GetFullPath(path);
		var root = Path.GetPathRoot(path);
		var current = root;

		foreach(var part in path[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
		{
			current = Path.Combine(current, part);
			FileSystemInfo info = Directory.Exists(current) ? new DirectoryInfo(current) : new FileInfo(current);

			if(info.LinkTarget != null)
				current = info.ResolveLinkTarget(true)?.FullName ?? throw new IOException(string.Format(Properties.Resources.Configuration_Xml_LinkResolutionFailed_Message, current));
		}

		return current;
	}

	internal static FormatException CreateException(string message, XmlConfigurationInstruction instruction, object argument = null) =>
		new(string.Format(message, argument ?? instruction?.Name, instruction?.FilePath, instruction?.LineNumber ?? 0, instruction?.LinePosition ?? 0));
}
