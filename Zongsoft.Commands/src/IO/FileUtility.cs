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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

internal static class FileUtility
{
	public static async ValueTask<object> OpenFileAsync(CommandContext context, FileMode mode, FileAccess access, FileShare share, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		List<string> paths;

		if(context.Arguments.IsEmpty)
		{
			var terminal = context.GetTerminal() ?? throw new CommandException(string.Format(Properties.Resources.Command_InteractiveArgumentsUnavailable_Message, context.Command.Name));

			var filePath = string.Empty;
			var prompt = (access & FileAccess.Write) == FileAccess.Write ?
				Properties.Resources.SaveFile_Prompt:
				Properties.Resources.OpenFile_Prompt;

			do
			{
				context.Output.Write(CommandOutletColor.DarkYellow, prompt);
				filePath = await terminal.Input.ReadLineAsync(cancellation).ConfigureAwait(false);

				if(filePath == null)
					return null;

				filePath = filePath.Trim();
			} while(string.IsNullOrEmpty(filePath));

			paths = [filePath];
		}
		else
		{
			paths = new List<string>(context.Arguments.Count);

			foreach(var argument in context.Arguments)
			{
				if(!string.IsNullOrWhiteSpace(argument))
					paths.Add(argument.Trim());
			}

			if(paths.Count == 0)
				throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);
		}

		var streams = new List<Stream>(paths.Count);

		try
		{
			foreach(var path in paths)
			{
				cancellation.ThrowIfCancellationRequested();
				streams.Add(await FileSystem.File.OpenAsync(
						path,
						mode,
						access,
						share, cancellation));
			}
		}
		catch(Exception failure)
		{
			try { await DisposeAsync(streams); }
			catch(Exception cleanup) { throw new AggregateException(failure, cleanup); }

			throw;
		}

		return streams.Count switch
		{
			0 => null,
			1 => streams[0],
			_ => streams.ToArray(),
		};
	}

	public static async ValueTask DisposeAsync(object output)
	{
		var streams = output is Stream stream ? [stream] : output as IEnumerable<Stream>;
		if(streams == null)
			return;

		List<Exception> errors = null;

		foreach(var item in streams)
		{
			if(item == null)
				continue;

			try { await item.DisposeAsync(); }
			catch(Exception failure) { (errors ??= []).Add(failure); }
		}

		if(errors != null)
			throw new AggregateException(errors);
	}

	public static async ValueTask SaveAsync(object output, object parameter, Encoding encoding, CancellationToken cancellation)
	{
		if(output == null || parameter == null)
			return;

		try
		{
			if(output is IEnumerable<Stream> streams)
			{
				foreach(var stream in streams)
				{
					if(stream != null)
						await WriteToStreamAsync(stream, parameter, encoding, cancellation);
				}
			}
			else if(output is Stream stream)
				await WriteToStreamAsync(stream, parameter, encoding, cancellation);
		}
		catch(Exception failure)
		{
			try { await DisposeAsync(output); }
			catch(Exception cleanup) { throw new AggregateException(failure, cleanup); }

			throw;
		}
	}

	private static async ValueTask WriteToStreamAsync(Stream destination, object parameter, Encoding encoding, CancellationToken cancellation)
	{
		encoding ??= Encoding.UTF8;

		switch(parameter)
		{
			case Stream stream:
				await stream.CopyToAsync(destination, cancellation);
				break;
			case TextReader reader:
				await using(var writer = new StreamWriter(destination, encoding, 4096, true))
				{
					var characters = new char[4096];
					int count;

					while((count = await reader.ReadAsync(characters.AsMemory(), cancellation)) > 0)
						await writer.WriteAsync(characters.AsMemory(0, count), cancellation);
				}

				break;
			case BinaryReader reader:
				await reader.BaseStream.CopyToAsync(destination, cancellation);
				break;
			case Memory<byte> memory:
				await destination.WriteAsync(memory, cancellation);
				break;
			case ReadOnlyMemory<byte> memory:
				await destination.WriteAsync(memory, cancellation);
				break;
			case IEnumerable<byte> bytes:
				foreach(var chunk in bytes.Chunk(8192))
					await destination.WriteAsync(chunk, cancellation);

				break;
			case string text:
				await destination.WriteAsync(encoding.GetBytes(text), cancellation);
				break;
			case StringBuilder text:
				await destination.WriteAsync(encoding.GetBytes(text.ToString()), cancellation);
				break;
			case IEnumerable<string> texts:
				foreach(var text in texts)
					await destination.WriteAsync(encoding.GetBytes(text), cancellation);

				break;
			case IEnumerable<StringBuilder> texts:
				foreach(var text in texts)
					await destination.WriteAsync(encoding.GetBytes(text.ToString()), cancellation);

				break;
		}
	}
}
