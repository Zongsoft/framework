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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.AI;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands;

[CommandOption(FORMAT_OPTION, 'f', typeof(ChatResponseFormat))]
[CommandOption(SESSION_OPTION, 'e', typeof(string))]
[CommandOption(STREAMING_OPTION, 's', typeof(bool), DefaultValue = false)]
[CommandOption(INTERACTIVE_OPTION, 'i', typeof(bool), DefaultValue = false)]
public class ChatCommand() : CommandBase<CommandContext>("Chat")
{
	#region 常量定义
	private const string FORMAT_OPTION      = "format";
	private const string SESSION_OPTION     = "session";
	private const string STREAMING_OPTION   = "streaming";
	private const string INTERACTIVE_OPTION = "interactive";
	#endregion

	#region 成员变量
	private readonly List<ChatMessage> _history = new();
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var service = (context.Find<IServiceAccessor<IChatService>>(true)?.Value) ??
			throw new CommandException("The chat service required by this command was not found.");

		var format = context.GetOptions().GetValue(FORMAT_OPTION, ChatResponseFormat.Object);
		var session = service.Sessions.Get(context.GetOptions().GetValue<string>(SESSION_OPTION));

		if(context.GetOptions().TryGetValue<bool>(INTERACTIVE_OPTION, out var interactive) && interactive)
		{
			if(context.GetOptions().Contains(STREAMING_OPTION))
				throw new CommandOptionException("The interactive chat mode does not support streaming responses.");

			await Chat(context, service);
			return _history;
		}

		if(context.GetOptions().GetValue<bool>(STREAMING_OPTION))
		{
			if(context.Arguments.IsEmpty)
				return Collections.Enumerable.EnumerateAsync<ChatResponseUpdate>(null, cancellation);

			var history = service.Sessions.Current?.History;

			if(history == null)
			{
				return format switch
				{
					ChatResponseFormat.Object => service.GetStreamingResponseAsync(GetMessage(context), null, cancellation),
					ChatResponseFormat.Text => service.GetStreamingResponseAsync(GetMessage(context), null, cancellation).Map(entry => entry.Text),
					_ => throw new CommandOptionException($"The response format '{format}' is not supported."),
				};
			}
			else
			{
				//将用户输入的内容添加到对话历史中
				history.Append(new ChatMessage(ChatRole.User, [.. context.Arguments.Select(argument => new TextContent(argument))]));

				return format switch
				{
					ChatResponseFormat.Object => new ChatSession.Response(history, service.GetStreamingResponseAsync(history, null, cancellation)),
					ChatResponseFormat.Text => new ChatSession.Response<string>(history, service.GetStreamingResponseAsync(history, null, cancellation), entry => entry.Text),
					_ => throw new CommandOptionException($"The response format '{format}' is not supported."),
				};
			}
		}

		if(context.Arguments.IsEmpty)
			return null;

		var message = await Dialogue(context, service);
		if(message == null || message.Contents.Count == 0)
			return null;

		return format switch
		{
			ChatResponseFormat.Object => message,
			ChatResponseFormat.Text => message.Text,
			_ => throw new CommandOptionException($"The response format '{format}' is not supported."),
		};
	}
	#endregion

	#region 私有方法
	private static async ValueTask Chat(CommandContext context, IChatService service)
	{
		var terminal = context.GetTerminal() ??
			throw new CommandException("The interactive chat can only run in a terminal environment.");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Cyan, Common.StringExtension.Justify("Welcome to the interactive chat room.", 50))
			.AppendLine(CommandOutletStyles.Blinking, CommandOutletColor.DarkGreen, Common.StringExtension.Justify("Type “exit” to leave the chat room.", 50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		terminal.Write(splash);

		var history = service.Sessions.Current?.History;

		while(true)
		{
			terminal.Write(CommandOutletStyles.Bold, CommandOutletColor.Cyan, "You:> ");

			var text = terminal.Input.ReadLine();
			if(string.IsNullOrWhiteSpace(text))
				continue;

			if(string.Equals("exit", text, StringComparison.OrdinalIgnoreCase) ||
			   string.Equals("quit", text, StringComparison.OrdinalIgnoreCase))
				break;

			history?.Append(new ChatMessage(ChatRole.User, text));

			var message = history == null ?
				await Dialogue(terminal, service, GetMessage(context)) :
				await Dialogue(terminal, service, history);

			if(message != null && message.Contents.Count > 0)
			{
				history?.Append(message);
				terminal.WriteLine();
			}
		}
	}

	private static async ValueTask<ChatMessage> Dialogue(CommandContext context, IChatService service)
	{
		var history = service.Sessions.Current?.History;
		history?.Append(GetMessage(context));

		var message = history == null ?
			await Dialogue(context.GetTerminal(), service, GetMessage(context)) :
			await Dialogue(context.GetTerminal(), service, history);

		if(message != null && message.Contents.Count > 0)
			history?.Append(message);

		return message;
	}

	private static async ValueTask<ChatMessage> Dialogue(ITerminal terminal, IChatClient client, params IEnumerable<ChatMessage> messages)
	{
		if(messages == null || !messages.Any())
			return default;

		ChatMessage result = null;
		var response = client.GetStreamingResponseAsync(messages);

		await foreach(var entry in response)
		{
			if(entry == null || entry.Contents.Count == 0)
				break;

			Populate(ref result, entry);

			terminal?.Write(entry.Text);

			if(entry.FinishReason.HasValue)
				terminal?.WriteLine(CommandOutletStyles.Bold | CommandOutletStyles.Blinking, CommandOutletColor.Magenta, $" ({entry.FinishReason.Value})");
		}

		terminal?.WriteLine();
		return result;
	}

	private static ChatMessage GetMessage(CommandContext context) => new(ChatRole.User, [.. context.Arguments.Select(argument => new TextContent(argument))]);
	private static bool Populate(ref ChatMessage message, ChatResponseUpdate entry)
	{
		if(entry == null || entry.Contents.Count == 0)
			return false;

		message ??= new ChatMessage
		{
			Role = entry.Role ?? ChatRole.Assistant,
			AuthorName = entry.AuthorName,
			MessageId = entry.MessageId,
			RawRepresentation = entry.RawRepresentation,
			AdditionalProperties = entry.AdditionalProperties,
		};

		if(entry.Contents != null && entry.Contents.Count > 0)
		{
			foreach(var content in entry.Contents)
				message.Contents.Add(content);
		}

		if(entry.AdditionalProperties != null && entry.AdditionalProperties.Count > 0)
		{
			message.AdditionalProperties ??= new();

			foreach(var property in entry.AdditionalProperties)
				message.AdditionalProperties.Add(property.Key, property.Value);
		}

		return true;
	}
	#endregion

	#region 嵌套子类
	public enum ChatResponseFormat
	{
		Object,
		Text,
	}
	#endregion
}
