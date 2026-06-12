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
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Web;

namespace Zongsoft.Upgrading.Web.Controllers;

[Area("Upgrading")]
[ControllerName("Upgrader")]
public class UpgraderController : ControllerBase
{
	[HttpGet("{name:required}/{edition?}")]
	public async Task<IActionResult> GetAsync(string name, string edition, [FromQuery]Platform platform, [FromQuery]Architecture architecture, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new BadHttpRequestException($"The '{nameof(name)}' parameter is required.", StatusCodes.Status400BadRequest);

		var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach(var pair in this.Request.Query)
			parameters[pair.Key] = pair.Value;

		foreach(var header in this.Request.Headers)
			parameters[header.Key] = header.Value;

		using var stream = new MemoryStream();
		await Release.SaveAsync(stream, Upgrader.GetAsync(name, edition, platform, architecture, parameters, cancellation), cancellation);
		return this.File(stream.ToArray(), "application/manifest+xml");
	}

	[HttpGet("Evaluators")]
	public IActionResult GetEvaluators()
	{
		var evaluators = new List<EvaluatorInfo>();

		foreach(var evaluator in Module.Current.Evaluators)
			evaluators.Add(new(evaluator.Name, evaluator.Title, evaluator.Description));

		return evaluators == null || evaluators.Count == 0 ? this.NoContent() : this.Ok(evaluators);
	}

	[HttpPost("[action]/{phase}")]
	public async Task TraceAsync(string phase, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(phase))
			throw new BadHttpRequestException($"The '{nameof(phase)}' parameter is required.");

		var message = this.Request.Query.TryGetValue("message", out var value) ? value.ToString() : null;

		if(this.Request.HasTextContentType() && this.Request.ContentLength > 0)
			message = await this.Request.ReadAsStringAsync(cancellation);

		var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach(var parameter in this.Request.Query)
			properties[parameter.Key] = parameter.Value;

		if(this.Request.HasFormContentType)
		{
			foreach(var field in this.Request.Form)
				properties[field.Key] = field.Value;
		}

		await Upgrader.TraceAsync(phase, message, properties, cancellation);
	}

	internal readonly struct EvaluatorInfo(string name, string title, string description)
	{
		public string Name { get; } = name;
		public string Title { get; } = title;
		public string Description { get; } = description;
	}
}
