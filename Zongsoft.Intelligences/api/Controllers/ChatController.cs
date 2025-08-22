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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Services;

namespace Zongsoft.Intelligences.Web.Controllers;

partial class CopilotController
{
	[ControllerName("Chats", false)]
	public class ChatController : ControllerBase
	{
		[HttpGet("/[area]/{name}/[controller]/[action]/{count?}")]
		public async IAsyncEnumerable<string> Fake(string name, int count = 10, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new BadHttpRequestException($"Unspecified the AI assistant.");

			for(int i = 0; i < count; i++)
			{
				await Task.Delay(1000, cancellation);
				yield return $"[{DateTime.Now:HH:mm:ss}] {name} - Fake response {i + 1}";
			}
		}

		#region 公共方法
		[HttpPost("/[area]/{name}/[controller]/[action]")]
		[HttpPost("/[area]/{name}/[controller]/{id}/[action]")]
		public async Task ChatAsync(string name, string id, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(name))
				throw new BadHttpRequestException($"Unspecified the AI assistant.");

			var content = await this.Request.ReadAsStringAsync(cancellation);
			if(string.IsNullOrWhiteSpace(content))
				throw new BadHttpRequestException($"Missing the chat content.");

			var copilot = CopilotManager.GetCopilot(name) ?? throw new BadHttpRequestException($"The specified '{name}' AI assistant was not found.");
			var session = string.IsNullOrEmpty(id) ? null : copilot.Chatting.Sessions.Get(id);
			var results = session != null ?
				session.ChatAsync(content, cancellation) :         //有对话历史
				copilot.Chatting.ChatAsync(content, cancellation); //无对话历史

			await this.Response.EnumerableAsync(results, cancellation);
		}
		#endregion
	}
}
