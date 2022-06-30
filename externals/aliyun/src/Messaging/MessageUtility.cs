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
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

using Zongsoft.Services;
using Zongsoft.Messaging;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	internal static class MessageUtility
	{
		#region 常量定义
		public const string QueueNotExist = "QueueNotExist";
		public const string MessageNotExist = "MessageNotExist";

		private static readonly Regex _error_regex = new Regex(@"\<(?'tag'(Code|Message))\>\s*(?<value>[^<>]+)\s*\<\/\k'tag'\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		#endregion

		public static MessageQueueMessage ResolveMessage(MessageQueue queue, Stream stream)
		{
			if(stream == null)
				return new MessageQueueMessage(queue, null);

			string id = null, ackId = null, md5 = null, body = null;
			DateTime? expires = null, enqueuedTime = null, dequeuedTime = null;
			int dequeuedCount = 0;
			byte priority = 0;

			var settings = new XmlReaderSettings()
			{
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
			};

			using(var reader = XmlReader.Create(stream, settings))
			{
				if(reader.MoveToContent() != XmlNodeType.Element)
					return new MessageQueueMessage(queue, null);

				while(reader.Read())
				{
					if(reader.NodeType != XmlNodeType.Element)
						continue;

					switch(reader.LocalName)
					{
						case "MessageId":
							id = Utility.Xml.ReadContentAsString(reader);
							break;
						case "ReceiptHandle":
							ackId = Utility.Xml.ReadContentAsString(reader);
							break;
						case "MessageBodyMD5":
							md5 = Utility.Xml.ReadContentAsString(reader);
							break;
						case "MessageBody":
							body = Utility.Xml.ReadContentAsString(reader);
							break;
						case "EnqueueTime":
							enqueuedTime = Utility.GetDateTimeFromEpoch(Utility.Xml.ReadContentAsString(reader));
							break;
						case "NextVisibleTime":
							expires = Utility.GetDateTimeFromEpoch(Utility.Xml.ReadContentAsString(reader));
							break;
						case "FirstDequeueTime":
							dequeuedTime = Utility.GetDateTimeFromEpoch(Utility.Xml.ReadContentAsString(reader));
							break;
						case "DequeueCount":
							dequeuedCount = Zongsoft.Common.Convert.ConvertValue<int>(Utility.Xml.ReadContentAsString(reader));
							break;
						case "Priority":
							priority = Zongsoft.Common.Convert.ConvertValue<byte>(Utility.Xml.ReadContentAsString(reader));
							break;
					}
				}
			}

			return new MessageQueueMessage(
				queue,
				id,
				string.IsNullOrWhiteSpace(body) ? null : Convert.FromBase64String(body),
				(delay, cancellation) => queue.AcknowledgeAsync(ackId, delay, cancellation));
		}

		public static MessageTopicInfo ResolveTopicInfo(Stream stream)
		{
			if(stream == null)
				return null;

			var settings = new XmlReaderSettings()
			{
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
			};

			using(var reader = XmlReader.Create(stream, settings))
			{
				if(reader.MoveToContent() != XmlNodeType.Element)
					return null;

				var info = new MessageTopicInfo();

				while(reader.Read())
				{
					if(reader.NodeType != XmlNodeType.Element)
						continue;

					switch(reader.LocalName)
					{
						case "TopicName":
							info.Name = Utility.Xml.ReadContentAsString(reader);
							break;
						case "CreateTime":
							info.CreatedTime = Utility.GetDateTimeFromEpoch(Utility.Xml.ReadContentAsString(reader));
							break;
						case "LastModifyTime":
							info.ModifiedTime = Utility.GetDateTimeFromEpoch(Utility.Xml.ReadContentAsString(reader));
							break;
						case "MessageRetentionPeriod":
							info.MessageRetentionPeriod = TimeSpan.FromSeconds(reader.ReadElementContentAsInt());
							break;
						case "MessageCount":
							info.MessageCount = Zongsoft.Common.Convert.ConvertValue<int>(Utility.Xml.ReadContentAsString(reader));
							break;
						case "LoggingEnabled":
							info.LoggingEnabled = Zongsoft.Common.Convert.ConvertValue<bool>(Utility.Xml.ReadContentAsString(reader));
							break;
					}
				}

				return info;
			}
		}

		internal static Options.MessagingOptions GetOptions()
		{
			return ApplicationContext.Current?.Configuration.GetOption<Options.MessagingOptions>("Externals/Aliyun/Messaging");
		}

		internal static string GetMessageResponseId(Stream stream)
		{
			if(stream == null)
				return null;

			var settings = new XmlReaderSettings()
			{
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
			};

			using(var reader = XmlReader.Create(stream, settings))
			{
				if(reader.MoveToContent() != XmlNodeType.Element)
					return null;

				while(reader.Read())
				{
					if(reader.NodeType != XmlNodeType.Element)
						continue;

					switch(reader.LocalName)
					{
						case "MessageId":
							return reader.ReadElementContentAsString();
					}
				}
			}

			return null;
		}
	}
}
