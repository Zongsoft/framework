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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Reflection;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Wechat
{
	public class ChannelMessager
	{
		#region 构造函数
		public ChannelMessager(Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			this.Account = account;
		}
		#endregion

		#region 公共属性
		public Account Account { get; }
		#endregion

		#region 公共方法
		public async ValueTask<IEnumerable<Template>> GetTemplatesAsync(CancellationToken cancellation = default)
		{
			var credential = await CredentialManager.GetCredentialAsync(this.Account, false, cancellation);

			if(string.IsNullOrEmpty(credential))
				return default;

			var response = await CredentialManager.Http.GetAsync($"/cgi-bin/template/get_all_private_template?access_token={credential}", cancellation);
			var result = await response.GetResultAsync<TemplateWrapper>(cancellation);
			return result.Entries.Select(template => template.ToTemplate());
		}

		public async ValueTask<string> SendAsync(string destination, string template, object data, string url = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(destination))
				throw new ArgumentNullException(nameof(destination));
			if(string.IsNullOrEmpty(template))
				throw new ArgumentNullException(nameof(template));

			var credential = await CredentialManager.GetCredentialAsync(this.Account, false, cancellation);

			if(string.IsNullOrEmpty(credential))
				return default;

			var parameter = new TemplateData(destination, template, data, url);
			var response = await CredentialManager.Http.PostAsync(
				$"/cgi-bin/message/template/send?access_token={credential}",
				new StringContent(JsonSerializer.Serialize(parameter, Json.Options), System.Text.Encoding.UTF8, "application/json"),
				cancellation);

			var result = await response.GetResultAsync<MessageResult>(cancellation);
			return result.MessageId.ToString();
		}
		#endregion

		#region 嵌套结构
		public struct Template
		{
			public Template(string name, string title, string content, string[] industries, string description = null)
			{
				this.Name = name;
				this.Title = title;
				this.Content = content;
				this.Industries = industries;
				this.Description = description;
			}

			public string Name { get; set; }
			public string Title { get; set; }
			public string Content { get; set; }
			public string Description { get; set; }
			public string[] Industries { get; set; }
		}

		private struct TemplateWrapper
		{
			[JsonPropertyName("template_list")]
			[SerializationMember("template_list")]
			public IEnumerable<TemplateEntry> Entries { get; set; }

			public struct TemplateEntry
			{
				[JsonPropertyName("template_id")]
				[SerializationMember("template_id")]
				public string Name { get; set; }

				public string Title { get; set; }

				public string Content { get; set; }

				public string Example { get; set; }

				[JsonPropertyName("primary_industry")]
				[SerializationMember("primary_industry")]
				public string PrimaryIndustry { get; set; }

				[JsonPropertyName("deputy_industry")]
				[SerializationMember("deputy_industry")]
				public string DeputyIndustry { get; set; }

				public Template ToTemplate()
				{
					var industries = string.IsNullOrEmpty(PrimaryIndustry) ? null :
						new string[] { this.PrimaryIndustry, this.DeputyIndustry };

					return new Template(this.Name, this.Title, this.Content, industries, Example);
				}
			}
		}

		private struct MessageResult
		{
			[JsonPropertyName("errcode")]
			[SerializationMember("errcode")]
			public int ErrorCode { get; set; }

			[JsonPropertyName("errmsg")]
			[SerializationMember("errmsg")]
			public string ErrorMessage { get; set; }

			[JsonPropertyName("msgid")]
			[SerializationMember("msgid")]
			public long MessageId { get; set; }
		}

		private struct TemplateData
		{
			public TemplateData(string destination, string template, object data, string url = null)
			{
				this.Destination = destination;
				this.Template = template;
				this.Color = null;
				this.Data = GetTemplateDictionary(data);
				this.Url = url;
			}

			[JsonPropertyName("touser")]
			[SerializationMember("touser")]
			public string Destination { get; }

			[JsonPropertyName("template_id")]
			[SerializationMember("template_id")]
			public string Template { get; }

			[JsonPropertyName("topcolor")]
			[SerializationMember("topcolor")]
			public string Color { get; set; }

			public string Url { get; set; }
			public IDictionary<string, TemplateDataEntry> Data { get; }

			private static IDictionary<string, TemplateDataEntry> GetTemplateDictionary(object data)
			{
				if(data == null)
					return null;

				var result = new Dictionary<string, TemplateDataEntry>();

				if(data is IEnumerable<KeyValuePair<string, string>> stringDictionary)
				{
					foreach(var entry in stringDictionary)
						result.Add(entry.Key, new TemplateDataEntry(entry.Value));

					return result;
				}

				if(data is IEnumerable<KeyValuePair<string, object>> objectDictionary)
				{
					foreach(var entry in objectDictionary)
						result.Add(entry.Key, new TemplateDataEntry(entry.Value?.ToString()));

					return result;
				}

				if(data is System.Collections.IDictionary dictionary)
				{
					foreach(System.Collections.DictionaryEntry entry in dictionary)
						result.Add(entry.Key.ToString(), new TemplateDataEntry(entry.Value?.ToString()));

					return result;
				}

				var properties = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
				foreach(var property in properties)
					result.Add(property.Name, new TemplateDataEntry(Reflector.GetValue(property, ref data)?.ToString()));

				var fields = data.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
				foreach(var field in fields)
					result.Add(field.Name, new TemplateDataEntry(Reflector.GetValue(field, ref data)?.ToString()));

				return result;
			}
		}

		private readonly struct TemplateDataEntry
		{
			public TemplateDataEntry(string value, string color = null)
			{
				this.Value = value;
				this.Color = string.IsNullOrEmpty(color) ? "#173177" : color;
			}

			public string Value { get; }
			public string Color { get; }
		}
		#endregion
	}
}
