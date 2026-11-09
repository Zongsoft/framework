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
			throw new BadHttpRequestException(string.Format(Properties.Resources.Command_ParameterRequired_Message, nameof(name)), StatusCodes.Status400BadRequest);

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
			throw new BadHttpRequestException(string.Format(Properties.Resources.Command_ParameterRequired_Message, nameof(phase)));

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
