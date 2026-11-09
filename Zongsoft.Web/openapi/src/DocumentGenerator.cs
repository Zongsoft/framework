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
using System.Collections.Generic;

using Microsoft.OpenApi;

using Zongsoft.Services;

namespace Zongsoft.Web.OpenApi;

public static partial class DocumentGenerator
{
	public static OpenApiDocument Generate(DocumentContext context) => Generate(context, ApplicationContext.Current?.Properties.GetValue<ControllerServiceDescriptorCollection>());
	public static OpenApiDocument Generate(DocumentContext context, ControllerServiceDescriptorCollection descriptors)
	{
		if(descriptors == null)
			return null;

		//确保文档对象已创建
		context.Document ??= CreateDocument();

		//添加环境名到扩展集
		context.Document.AddExtension("x-environment", Extensions.Helper.String(ApplicationContext.Current.Environment.Name));
		context.Document.AddExtension("x-scalar-active-environment", Extensions.Helper.String(ApplicationContext.Current.Environment.Name));

		//生成环境列表
		context.GenerateEnvironments();

		//生成服务器列表
		context.GenerateServers();

		//生成安全方案列表
		context.GenerateSecuritySchemes();

		//生成API路径列表
		context.GeneratePaths(descriptors);

		return context.Document;

		static OpenApiDocument CreateDocument() => new()
		{
			Info = new()
			{
				Title = ApplicationContext.Current.Title,
				Description = ApplicationContext.Current.Description,
				Version = ApplicationContext.Current.Version.ToString(),
			},
			Tags = new HashSet<OpenApiTag>(),
			Components = new()
			{
				Schemas = new Dictionary<string, IOpenApiSchema>(),
				Responses = new Dictionary<string, IOpenApiResponse>(),
				RequestBodies = new Dictionary<string, IOpenApiRequestBody>(),
			},
		};
	}
}
