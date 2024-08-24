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
using System.Text;
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.IO.Commands
{
	internal static class FileUtility
	{
		public static object OpenFile(CommandContext context, FileMode mode, FileAccess access, FileShare share)
		{
			ICollection<string> paths = null;

			if(context.Expression.Arguments.Length == 0)
			{
				if(!(context is Terminals.TerminalCommandContext terminalContext))
					throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

				var filePath = string.Empty;
				var prompt = (access & FileAccess.Write) == FileAccess.Write ?
					Properties.Resources.Text_SaveFile_Prompt:
					Properties.Resources.Text_OpenFile_Prompt;

				do
				{
					context.Output.Write(CommandOutletColor.DarkYellow, prompt);
					filePath = terminalContext.Terminal.Input.ReadLine().Trim();
				} while(string.IsNullOrEmpty(filePath));

				paths = new string[] { filePath };
			}
			else
			{
				paths = new List<string>(context.Expression.Arguments.Length);

				foreach(var argument in context.Expression.Arguments)
				{
					if(!string.IsNullOrWhiteSpace(argument))
						paths.Add(argument.Trim());
				}

				if(paths.Count == 0)
					throw new CommandException(Properties.Resources.Text_Command_MissingArguments);
			}

			var streams = new List<Stream>(paths.Count);

			try
			{
				foreach(var path in paths)
				{
					streams.Add(FileSystem.File.Open(
							path,
							mode,
							access,
							share));
				}
			}
			catch
			{
				foreach(var stream in streams)
				{
					if(stream != null)
						stream.Dispose();
				}

				throw;
			}

			return streams.Count switch
			{
				0 => null,
				1 => streams[0],
				_ => streams.ToArray(),
			};
		}

		public static void Save(object output, object parameter, Encoding encoding = null)
		{
			if(output == null || parameter == null)
				return;

			if(output is IEnumerable<Stream> streams)
			{
				foreach(var stream in streams)
				{
					if(stream != null)
						WriteToStream(stream, parameter);
				}
			}
			else if(output is Stream stream)
			{
				WriteToStream(stream, parameter);
			}
		}

		private static void WriteToStream(Stream destination, object parameter, Encoding encoding = null)
		{
			encoding ??= Encoding.UTF8;

			switch(parameter)
			{
				case Stream stream:
					stream.CopyTo(destination);
					break;
				case TextReader textReader:
					textReader.CopyTo(destination, encoding);
					break;
				case BinaryReader binaryReader:
					binaryReader.CopyTo(destination);
					break;
				case Memory<byte> memory:
					destination.Write(memory.Span);
					break;
				case ReadOnlyMemory<byte> memory:
					destination.Write(memory.Span);
					break;
				case IEnumerable<byte> bytes:
					if(bytes.GetType().IsArray)
					{
						var array = (byte[])bytes;
						destination.Write(array, 0, array.Length);
					}
					else
					{
						foreach(var byteValue in bytes)
						{
							destination.WriteByte(byteValue);
						}
					}

					break;
				case string str:
					var buffer1 = encoding.GetBytes(str);
					destination.Write(buffer1, 0, buffer1.Length);
					break;
				case IEnumerable<string> strs:
					foreach(var str in strs)
					{
						var buffer1x = encoding.GetBytes(str);
						destination.Write(buffer1x, 0, buffer1x.Length);
					}
					break;
				case StringBuilder text:
					var buffer2 = encoding.GetBytes(text.ToString());
					destination.Write(buffer2, 0, buffer2.Length);
					break;
				case IEnumerable<StringBuilder> texts:
					foreach(var text in texts)
					{
						var buffer2x = encoding.GetBytes(text.ToString());
						destination.Write(buffer2x, 0, buffer2x.Length);
					}
					break;
			}
		}
	}
}
