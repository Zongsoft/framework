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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences.Web library.
 *
 * The Zongsoft.Intelligences.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;

namespace Zongsoft.Intelligences.Web.Controllers;

partial class AssistantController
{
	[ControllerName("Chats")]
	public class ChatController : ControllerBase
	{
		#region 公共方法
		[HttpGet("/[area]/{name}/[controller]/{id?}")]
		public IActionResult Get(string name, string id)
		{
			if(string.IsNullOrEmpty(name))
				return this.BadRequest($"Unspecified the AI assistant.");

			var assistant = AssistantManager.GetAssistant(name);
			if(assistant == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			if(string.IsNullOrEmpty(id))
				return this.Ok(assistant.Chatting.Sessions.Select(Map));

			var session = assistant.Chatting.Sessions.Get(id);
			return session == null ?
				this.NotFound($"The specified '{id}' chat session does not exist.") : this.Ok(Map(session));

			static object Map(IChatSession session) => new
			{
				session.Identifier,
				session.Creation,
				session.Options.Expiration,
				session.History.Count,
				session.Summary,
			};
		}

		[HttpPost("/[area]/{name}/[controller]/{id?}")]
		public IActionResult Open(string name, string id)
		{
			if(string.IsNullOrEmpty(name))
				return this.BadRequest($"Unspecified the AI assistant.");

			var assistant = AssistantManager.GetAssistant(name);
			if(assistant == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			var session = string.IsNullOrEmpty(id) ?
				assistant.Chatting.Sessions.Create() :
				assistant.Chatting.Sessions.Get(id);

			return session == null ?
				this.NotFound($"The specified '{id}' chat session does not exist.") : this.Content(session.Identifier);
		}

		[HttpDelete("/[area]/{name}/[controller]/{id}")]
		public IActionResult Exit(string name, string id)
		{
			if(string.IsNullOrEmpty(name))
				throw new BadHttpRequestException($"Unspecified the AI assistant.");
			if(string.IsNullOrEmpty(id))
				throw new BadHttpRequestException($"Unspecified the chat session identifier.");

			var assistant = AssistantManager.GetAssistant(name);
			if(assistant == null)
				return this.NotFound($"The specified '{name}' AI assistant was not found.");

			var session = assistant.Chatting.Sessions.Abandon(id);
			return session == null ?
				this.NotFound($"The specified '{id}' chat session does not exist.") : this.NoContent();
		}

		[HttpPost("/[area]/{name}/[controller]/[action]")]
		[HttpPost("/[area]/{name}/[controller]/{id}/[action]")]
		public async Task ChatAsync(string name, string id, [FromQuery]string role, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new BadHttpRequestException($"Unspecified the AI assistant.");

			var content = await this.Request.ReadAsStringAsync(cancellation);
			if(string.IsNullOrWhiteSpace(content))
				throw new BadHttpRequestException($"Missing the chat content.");

			var assistant = AssistantManager.GetAssistant(name) ?? throw new BadHttpRequestException($"The specified '{name}' AI assistant was not found.");
			var session = string.IsNullOrEmpty(id) ? null : assistant.Chatting.Sessions.Get(id);
			var results = session != null ?
				session.ChatAsync(role, content, cancellation) :           //有对话历史
				assistant.Chatting.ChatAsync(role, content, cancellation); //无对话历史

			await this.Response.EnumerableAsync(results, cancellation);
		}
		#endregion

		#region 嵌套子类
		[ControllerName("History")]
		public class HistoryController : ControllerBase
		{
			[HttpGet("/AI/Assistants/{name}/Chats/{id}/[controller]")]
			public IActionResult Get(string name, string id)
			{
				if(string.IsNullOrEmpty(name))
					return this.BadRequest($"Unspecified the AI assistant.");
				if(string.IsNullOrEmpty(id))
					return this.BadRequest($"Unspecified the chat session identifier.");

				var assistant = AssistantManager.GetAssistant(name);
				if(assistant == null)
					return this.NotFound($"The specified '{name}' AI assistant was not found.");

				var session = assistant.Chatting.Sessions.Get(id);
				if(session == null)
					return this.NotFound($"The specified '{id}' chat session does not exist.");

				return this.Ok(session.History.Select(entry => new { entry.Role, entry.Text }));
			}

			[HttpDelete("/AI/Assistants/{name}/Chats/{id}/[controller]")]
			public IActionResult Clear(string name, string id)
			{
				if(string.IsNullOrEmpty(name))
					return this.BadRequest($"Unspecified the AI assistant.");
				if(string.IsNullOrEmpty(id))
					return this.BadRequest($"Unspecified the chat session identifier.");

				var assistant = AssistantManager.GetAssistant(name);
				if(assistant == null)
					return this.NotFound($"The specified '{name}' AI assistant was not found.");

				var session = assistant.Chatting.Sessions.Get(id);
				if(session == null)
					return this.NotFound($"The specified '{id}' chat session does not exist.");

				session.History.Clear();
				return this.NoContent();
			}
		}
		#endregion
	}
}
