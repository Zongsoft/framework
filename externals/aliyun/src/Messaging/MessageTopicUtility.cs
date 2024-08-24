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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	internal static class MessageTopicUtility
	{
		public static ICertificate GetCertificate(string name)
		{
			var options = MessageUtility.GetOptions();
			var certificate = string.Empty;

			if(options.Topics.TryGet(name, out var option))
				certificate = option.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				certificate = options.Topics.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				return Aliyun.Options.GeneralOptions.Instance.Certificates.Default;

			return Aliyun.Options.GeneralOptions.Instance.Certificates.Get(certificate);
		}

		public static string GetRequestUrl(string topicName, params string[] parts)
		{
			var options = MessageUtility.GetOptions();
			var region = options.Topics.Region ?? Aliyun.Options.GeneralOptions.Instance.Name;

			if(options.Topics.TryGet(topicName, out var option) && option.Region.HasValue)
				region = option.Region.Value;

			var center = ServiceCenter.GetInstance(region, Aliyun.Options.GeneralOptions.Instance.IsIntranet);

			var path = parts == null ? string.Empty : string.Join("/", parts);

			if(string.IsNullOrEmpty(path))
				return string.Format("http://{0}.{1}/topics/{2}", options.Name, center.Path, topicName);
			else
				return string.Format("http://{0}.{1}/topics/{2}/{3}", options.Name, center.Path, topicName, path);
		}
	}
}
