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

	internal static string NormalizeOptionPath(string path) => string.IsNullOrWhiteSpace(path) ? string.Empty : path.Trim('/', ' ', '\t').Replace('/', ':');
	internal static FormatException CreateException(string message, XmlConfigurationInstruction instruction, object argument = null) =>
		new(string.Format(message, argument ?? instruction?.Name, instruction?.FilePath, instruction?.LineNumber ?? 0, instruction?.LinePosition ?? 0));
}
