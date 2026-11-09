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

namespace Zongsoft.Externals.Aliyun.Messaging;

internal static class MessageTopicUtility
{
	public static ICertificate GetCertificate(string name)
	{
		var options = MessageUtility.GetOptions();
		var certificate = string.Empty;

		if(options.Topics.TryGetValue(name, out var option))
			certificate = option.Certificate;

		if(string.IsNullOrWhiteSpace(certificate))
			certificate = options.Topics.Certificate;

		if(string.IsNullOrWhiteSpace(certificate))
			return Aliyun.Options.GeneralOptions.Instance.Certificates.Default;

		return Aliyun.Options.GeneralOptions.Instance.Certificates.GetCertificate(certificate);
	}

	public static string GetRequestUrl(string topicName, params string[] parts)
	{
		var options = MessageUtility.GetOptions();
		var region = options.Topics.Region ?? Aliyun.Options.GeneralOptions.Instance.Name;

		if(options.Topics.TryGetValue(topicName, out var option) && option.Region.HasValue)
			region = option.Region.Value;

		var center = ServiceCenter.GetInstance(region, Aliyun.Options.GeneralOptions.Instance.IsIntranet);

		var path = parts == null ? string.Empty : string.Join("/", parts);

		if(string.IsNullOrEmpty(path))
			return string.Format("http://{0}.{1}/topics/{2}", options.Name, center.Path, topicName);
		else
			return string.Format("http://{0}.{1}/topics/{2}/{3}", options.Name, center.Path, topicName, path);
	}
}
