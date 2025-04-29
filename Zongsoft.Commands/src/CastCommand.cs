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
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Commands
{
	[DisplayName("Text.CastCommand.Name")]
	[Description("Text.CastCommand.Description")]
	[CommandOption(KEY_TYPE_OPTION, typeof(CastType), CastType.Raw, "Text.CastCommand.Options.Type")]
	[CommandOption(KEY_ENCODING_OPTION, typeof(Encoding), null, "Text.CastCommand.Options.Encoding")]
	[CommandOption(KEY_COUNT_OPTION, typeof(int), 0, "Text.CastCommand.Options.Count")]
	[CommandOption(KEY_OFFSET_OPTION, typeof(int), 0, "Text.CastCommand.Options.Offset")]
	public class CastCommand : CommandBase<CommandContext>
	{
		#region 枚举定义
		/// <summary>
		/// 表示转换类型的枚举。
		/// </summary>
		public enum CastType
		{
			Raw,
			Hex,
			Base64,
		}
		#endregion

		#region 常量定义
		private const string KEY_COUNT_OPTION = "count";
		private const string KEY_OFFSET_OPTION = "offset";
		private const string KEY_TYPE_OPTION = "type";
		private const string KEY_ENCODING_OPTION = "encoding";

		private const int BUFFER_SIZE = 1024;
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Parameter == null)
				return null;

			var type = context.Expression.Options.GetValue<CastType>(KEY_TYPE_OPTION);
			var encoding = context.Expression.Options.GetValue<Encoding>(KEY_ENCODING_OPTION) ?? Encoding.UTF8;
			var count = context.Expression.Options.GetValue<int>(KEY_COUNT_OPTION);
			var offset = context.Expression.Options.GetValue<int>(KEY_OFFSET_OPTION);

			if(offset < 0)
				throw new CommandOptionValueException(KEY_OFFSET_OPTION, offset);

			//根据当前命令的参数获取对应的读取器
			var reader = this.GetReader(context.Parameter, encoding);

			if(reader == null)
				throw new NotSupportedException();

			object result;

			switch(type)
			{
				case CastType.Raw:
					result = this.ConvertToRawString(reader,
						offset,
						count,
						encoding, context.Expression.Arguments);
					break;
				case CastType.Hex:
					result = this.ConvertToHexString(reader,
						offset,
						count,
						context.Expression.Arguments);
					break;
				case CastType.Base64:
					result = this.ConvertToBase64(reader,
						offset,
						count,
						context.Expression.Arguments);
					break;
				default:
					return null;
			}

			if(result != null)
				context.Output.Write(result);

			return result;
		}
		#endregion

		#region 私有方法
		private BinaryReader GetReader(object source, Encoding encoding)
		{
			MemoryStream ms;

			switch(source)
			{
				case BinaryReader binaryReader:
					return binaryReader;
				case Stream stream:
					return new BinaryReader(stream, encoding);
				case IEnumerable<byte> bytes:
					ms = new MemoryStream(System.Linq.Enumerable.ToArray(bytes));
					return new BinaryReader(ms, encoding, false);
				case string str:
					ms = new MemoryStream(encoding.GetBytes(str));
					return new BinaryReader(ms, encoding, false);
				case StringBuilder sb:
					ms = new MemoryStream(encoding.GetBytes(sb.ToString()));
					return new BinaryReader(ms, encoding, false);
				default:
					return null;
			}
		}

		private bool Move(BinaryReader reader, int offset)
		{
			try
			{
				for(int i = 0; i < offset; i++)
				{
					reader.ReadByte();
				}
			}
			catch
			{
				return false;
			}

			return true;
		}

		private string ConvertToRawString(BinaryReader reader, int offset, int count, Encoding encoding, string[] args)
		{
			var buffer = new byte[BUFFER_SIZE];
			var bufferRead = 0;
			var text = new StringBuilder();

			//如果偏移量大于零则移动读取器指针，如果移动失败（超出末尾）则返回失败
			if(offset > 0 && (!this.Move(reader, offset)))
				return null;

			//如果指定了要返回的字节数则需要进行限制处理
			var isLimit = count > 0;

			while((!isLimit || (isLimit && count > 0)) &&
			      (bufferRead = reader.Read(buffer, 0, isLimit ? Math.Min(count, buffer.Length) : buffer.Length)) > 0)
			{
				text.Append(encoding.GetString(buffer, 0, bufferRead));

				if(count > 0)
					count -= bufferRead;
			}

			return text.ToString();
		}

		private string ConvertToBase64(BinaryReader reader, int offset, int count, string[] args)
		{
			var buffer = new byte[BUFFER_SIZE];
			var bufferRead = 0;
			var text = new StringBuilder();

			//如果偏移量大于零则移动读取器指针，如果移动失败（超出末尾）则返回失败
			if(offset > 0 && (!this.Move(reader, offset)))
				return null;

			//如果指定了要返回的字节数则需要进行限制处理
			var isLimit = count > 0;

			while((!isLimit || (isLimit && count > 0)) &&
				  (bufferRead = reader.Read(buffer, 0, isLimit ? Math.Min(count, buffer.Length) : buffer.Length)) > 0)
			{
				text.Append(System.Convert.ToBase64String(buffer, 0, bufferRead));

				if(count > 0)
					count -= bufferRead;
			}

			return text.ToString();
		}

		private string ConvertToHexString(BinaryReader reader, int offset, int count, string[] args)
		{
			var buffer = new byte[BUFFER_SIZE];
			var bufferRead = 0;
			var total = 0;
			var chars = new byte[16];
			var text = new StringBuilder();

			//如果偏移量大于零则移动读取器指针，如果移动失败（超出末尾）则返回失败
			if(offset > 0 && (!this.Move(reader, offset)))
				return null;

			//如果指定了要返回的字节数则需要进行限制处理
			var isLimit = count > 0;

			while((!isLimit || (isLimit && count > 0)) &&
				  (bufferRead = reader.Read(buffer, 0, isLimit ? Math.Min(count, buffer.Length) : buffer.Length)) > 0)
			{
				if(count > 0)
					count -= bufferRead;

				//如果指定了以原生格式输出
				if(args != null && args.Length > 0 && Array.Exists(args, arg => string.Equals(arg, "raw", StringComparison.OrdinalIgnoreCase)))
				{
					text.Append(Common.Convert.ToHexString(buffer, 0, bufferRead, '-'));
					continue;
				}

				for(int i = 0; i < bufferRead; i++)
				{
					var column = (total + i) % 16;

					if(column == 0)
					{
						//输出上一行的ASCII码字符部分
						if(total + i > 0)
							text.AppendLine("\t" + Encoding.ASCII.GetString(chars));

						//输出地址位置
						text.Append("[" + (total + offset + i).ToString("X8") + "]  ");
					}

					//如果是不可显示字符则将其转换成特定可见字符
					if(buffer[i] < 32 || buffer[i] > 126)
						chars[column] = 63; //问号的ASCII码值
					else
						chars[column] = buffer[i];

					//输出字节的十六进制数值
					text.Append(buffer[i].ToString("x2") + " ");
				}

				//累加总计的读取字节数
				total += bufferRead;
			}

			//计算末尾行的字节数
			var tail = total > 0 ? total % 16 : -1;

			if(tail >= 0)
			{
				//如果末尾字节数为零，则表示其为满行即16个字节
				if(tail == 0)
					tail = 16;

				//填充末尾行的字节数值部分
				text.Append(new string(' ', (16 - tail) * 3));
				//输出末尾行的ASCII字符部分
				text.AppendLine("\t" + Encoding.ASCII.GetString(chars, 0, tail));
			}

			return text.ToString();
		}
		#endregion
	}
}
