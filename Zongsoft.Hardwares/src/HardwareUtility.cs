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
 * This file is part of Zongsoft.Hardwares library.
 *
 * The Zongsoft.Hardwares is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Hardwares is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Hardwares library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace Zongsoft.Hardwares;

internal static class HardwareUtility
{
	private static readonly string[] _unknowns =
	[
		"unknown",
		"n/a",
		"na",
		"none",
		"null",
		"not available",
		"not applicable",
		"not specified",
		"not provided",
		"to be filled by o.e.m.",
		"to be filled by oem",
		"default string",
		"system serial number",
		"system product name",
		"system manufacturer",
		"base board",
	];

	public static string Normalize(object value)
	{
		if(value == null)
			return null;

		if(value is string text)
		{
			text = text.Trim();

			if(text.Length == 0)
				return null;

			var comparison = StringComparison.OrdinalIgnoreCase;

			if(_unknowns.Any(unknown => string.Equals(text, unknown, comparison)))
				return null;

			return text;
		}

		if(value is Array array)
		{
			if(array.Length == 0)
				return null;

			var result = string.Join(", ", array.Cast<object>().Select(Normalize).Where(item => !string.IsNullOrEmpty(item)));
			return string.IsNullOrEmpty(result) ? null : result;
		}

		return value.ToString();
	}

	public static void Add(List<IO.Hardwares.HardwareProperty> properties, string name, object value, string description = null)
	{
		if(properties == null || string.IsNullOrEmpty(name))
			return;

		var result = NormalizeValue(value);

		if(result == null)
			return;

		if(properties.Any(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
			return;

		properties.Add(new(name, result, description));
	}

	public static string Coalesce(params object[] values)
	{
		if(values == null || values.Length == 0)
			return null;

		for(int i = 0; i < values.Length; i++)
		{
			var value = Normalize(values[i]);

			if(!string.IsNullOrEmpty(value))
				return value;
		}

		return null;
	}

	public static string ReadFirst(params string[] paths)
	{
		if(paths == null)
			return null;

		for(int i = 0; i < paths.Length; i++)
		{
			var value = ReadText(paths[i]);

			if(!string.IsNullOrEmpty(value))
				return value;
		}

		return null;
	}

	public static string ReadText(string path)
	{
		if(string.IsNullOrEmpty(path))
			return null;

		try
		{
			return File.Exists(path) ? Normalize(File.ReadAllText(path)) : null;
		}
		catch(IOException)
		{
			return null;
		}
		catch(UnauthorizedAccessException)
		{
			return null;
		}
	}

	public static CommandResult Execute(string fileName, string arguments, int timeout = 5000)
	{
		if(string.IsNullOrEmpty(fileName))
			return CommandResult.Empty;

		try
		{
			using var process = new Process()
			{
				StartInfo = new ProcessStartInfo(fileName, arguments ?? string.Empty)
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
					StandardOutputEncoding = Encoding.UTF8,
					StandardErrorEncoding = Encoding.UTF8,
				},
			};

			if(!process.Start())
				return CommandResult.Empty;

			var output = process.StandardOutput.ReadToEndAsync();
			var error = process.StandardError.ReadToEndAsync();

			if(!process.WaitForExit(timeout))
			{
				try
				{
					process.Kill(true);
				}
				catch(InvalidOperationException) { }

				return CommandResult.Empty;
			}

			return new CommandResult(process.ExitCode, output.GetAwaiter().GetResult(), error.GetAwaiter().GetResult());
		}
		catch(InvalidOperationException)
		{
			return CommandResult.Empty;
		}
		catch(System.ComponentModel.Win32Exception)
		{
			return CommandResult.Empty;
		}
		catch(IOException)
		{
			return CommandResult.Empty;
		}
	}

	public static ulong? ParseBytes(object value)
	{
		var text = Normalize(value);

		if(string.IsNullOrEmpty(text))
			return null;

		if(ulong.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var bytes))
			return bytes;

		var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if(parts.Length < 2 || !decimal.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
			return null;

		var unit = parts[1].Trim().ToUpperInvariant();
		var scale = unit switch
		{
			"B" or "BYTE" or "BYTES" => 1M,
			"KB" or "KIB" => 1024M,
			"MB" or "MIB" => 1024M * 1024M,
			"GB" or "GIB" => 1024M * 1024M * 1024M,
			"TB" or "TIB" => 1024M * 1024M * 1024M * 1024M,
			_ => 0M,
		};

		return scale <= 0 ? null : (ulong)(size * scale);
	}

	public static ulong? ParseKilobytes(object value)
	{
		var text = Normalize(value);

		if(string.IsNullOrEmpty(text))
			return null;

		var number = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
		return ulong.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result * 1024UL : null;
	}

	private static object NormalizeValue(object value)
	{
		if(value == null)
			return null;

		if(value is string)
			return Normalize(value);

		if(value is Array)
			return Normalize(value);

		return value;
	}
}

internal readonly record struct CommandResult(int ExitCode, string Output, string Error)
{
	public static readonly CommandResult Empty = new(-1, null, null);
	public bool Succeeded => this.ExitCode == 0 && !string.IsNullOrWhiteSpace(this.Output);
}
