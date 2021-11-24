/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

using Zongsoft.Services;

namespace Zongsoft.Security.Commands
{
	[CommandOption(TYPE_OPTION, typeof(RSAKeyType))]
	[CommandOption(FORMAT_OPTION, typeof(RSAKeyFormat))]
	public class RSAImportCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string TYPE_OPTION = "type";
		private const string FORMAT_OPTION = "format";
		#endregion

		#region 构造函数
		public RSAImportCommand() : base("Import") { }
		public RSAImportCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			var rsa = RSACommand.FindRSA(context.CommandNode);

			if(rsa == null)
				throw new CommandException("Missing the required RSA.");

			switch(context.Expression.Options.GetValue<RSAKeyType>(TYPE_OPTION))
			{
				case RSAKeyType.All:
					if(TryGetInputXml(context.Parameter, out var xml))
						rsa.FromXmlString(xml);
					break;
				case RSAKeyType.Public:
					if(TryGetInput(context.Parameter, out var publicKey))
						rsa.ImportRSAPublicKey(publicKey, out _);

					break;
				case RSAKeyType.Private:
					if(TryGetInput(context.Parameter, out var privateKey))
					{
						if(context.Expression.Options.GetValue<RSAKeyFormat>(FORMAT_OPTION) == RSAKeyFormat.Pkcs8)
							rsa.ImportPkcs8PrivateKey(privateKey, out _);
						else
							rsa.ImportRSAPrivateKey(privateKey, out _);
					}
					break;
			}

			return rsa;
		}
		#endregion

		private static bool TryGetInputXml(object parameter, out string result)
		{
			if(parameter == null)
			{
				result = null;
				return false;
			}

			if(parameter is Stream stream)
			{
				var reader = new StreamReader(stream, System.Text.Encoding.UTF8);
				result = reader.ReadToEnd();
				return !string.IsNullOrEmpty(result);
			}

			result = parameter as string;
			return !string.IsNullOrEmpty(result);
		}

		private static bool TryGetInput(object parameter, out ReadOnlySpan<byte> result)
		{
			if(parameter == null)
			{
				result = ReadOnlySpan<byte>.Empty;
				return false;
			}

			if(parameter is Stream stream)
			{
				var memory = new MemoryStream();
				stream.CopyTo(memory);
				result = memory.ToArray().AsSpan();
				return true;
			}

			result = parameter switch
			{
				byte[] bytes => bytes.AsSpan(),
				Memory<byte> memory => memory.Span,
				ReadOnlyMemory<byte> memory => memory.Span,
				_ => default,
			};

			return !result.IsEmpty;
		}
	}
}
