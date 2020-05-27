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
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	public class Phone
	{
		#region 常量定义
		private const string PHONE_VOICE_DOMAIN   = "dyvmsapi.aliyuncs.com";
		private const string PHONE_MESSAGE_DOMAIN = "dysmsapi.aliyuncs.com";
		#endregion

		#region 成员字段
		private Options.IConfiguration _configuration;
		private readonly ConcurrentDictionary<string, HttpClient> _pool;
		#endregion

		#region 构造函数
		public Phone()
		{
			_pool = new ConcurrentDictionary<string, HttpClient>();

			this.Caller = new PhoneCallCommand(this);
			this.Sender = new PhoneSendCommand(this);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置电信服务配置信息。
		/// </summary>
		public Options.IConfiguration Configuration
		{
			get
			{
				return _configuration;
			}
			set
			{
				_configuration = value ?? throw new ArgumentNullException();
			}
		}

		/// <summary>
		/// 获取电话语音呼叫命令。
		/// </summary>
		public ICommand Caller
		{
			get;
		}

		/// <summary>
		/// 获取手机短信发送命令。
		/// </summary>
		public ICommand Sender
		{
			get;
		}
		#endregion

		#region 公共方法
		/// <summary>
		/// 拨打电话到指定的手机号。
		/// </summary>
		/// <param name="name">指定的语音模板名称。</param>
		/// <param name="destination">目标手机号。</param>
		/// <param name="parameter">语音呼叫模板参数对象。</param>
		/// <param name="extra">扩展附加数据，通常表示特定的业务数据。</param>
		/// <returns>返回的语音呼叫结果信息。</returns>
		public async Task<Result> CallAsync(string name, string destination, object parameter, string extra = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(string.IsNullOrWhiteSpace(destination))
				throw new ArgumentNullException(nameof(destination));

			//确认当前电信服务的配置
			var configuration = this.EnsureConfiguration();

			if(configuration.Voice == null)
				throw new InvalidOperationException($"Missing required telecom voice option.");

			if(configuration.Voice.Numbers == null || configuration.Voice.Numbers.Length == 0)
				throw new InvalidOperationException($"Missing required telecom voice phone numbers in the option.");

			var caller = configuration.Voice.Numbers[0];

			if(configuration.Voice.Numbers.Length > 1)
				caller = configuration.Voice.Numbers[Common.Randomizer.GenerateInt32() % configuration.Voice.Numbers.Length];

			//获取指定名称的语音模板配置，如果获取失败则抛出异常
			if(!configuration.Message.Templates.TryGet(name, out var template))
				throw new InvalidOperationException($"The specified '{name}' sms template is not existed.");

			//获取当前电信服务的凭证
			var certificate = this.GetCertificate();

			//生成服务请求的公共头集
			var headers = this.GetCommonHeaders("SingleCallByTts");

			//添加必须的语音服务参数
			headers.Add("TtsCode", template.Code);
			headers.Add("CalledNumber", destination);
			headers.Add("CalledShowNumber", caller.Trim());

			if(parameter != null)
			{
				if(parameter is string || parameter is System.Text.StringBuilder)
					headers.Add("TtsParam", parameter.ToString());
				else
					headers.Add("TtsParam", Serializer.Json.Serialize(parameter, TextSerializationSettings.Camel));
			}

			if(!string.IsNullOrWhiteSpace(extra))
				headers.Add("OutId", extra.Trim());

			//构建语音拨号的HTTP请求消息包
			var request = new HttpRequestMessage(HttpMethod.Get, "http://" + PHONE_VOICE_DOMAIN + "?" + Utility.GetQueryString(headers));
			request.Headers.Accept.TryParseAdd("application/json");

			//获取当前实例关联的HTTP客户端程序
			var http = this.GetHttpClient(certificate);

			//提交语音拨号请求
			var response = await http.SendAsync(request);

			//确认返回状态码是成功的
			response.EnsureSuccessStatusCode();

			return await this.GetResultAsync(response.Content);
		}

		/// <summary>
		/// 发送短信到指定的手机号，支持多发。
		/// </summary>
		/// <param name="name">指定的短信模板名称。</param>
		/// <param name="destinations">目标手机号码集。</param>
		/// <param name="parameter">短信模板参数对象。</param>
		/// <param name="scheme">短信签名，如果为空或空字符串则使用指定模板中的签名。</param>
		/// <param name="extra">扩展附加数据，通常表示特定的业务数据。</param>
		/// <returns>返回的短信发送结果信息。</returns>
		public async Task<Result> SendAsync(string name, IEnumerable<string> destinations, object parameter, string scheme = null, string extra = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			if(destinations == null)
				throw new ArgumentNullException(nameof(destinations));

			var target = string.Join(",", destinations);

			if(string.IsNullOrWhiteSpace(target))
				throw new InvalidOperationException("Missing destination for the send of telecom sms.");

			//确认当前电信服务的配置
			var configuration = this.EnsureConfiguration();

			if(configuration.Message == null)
				throw new InvalidOperationException($"Missing required telecom sms option.");

			//获取指定名称的短信模板配置，如果获取失败则抛出异常
			if(!configuration.Message.Templates.TryGet(name, out var template))
				throw new InvalidOperationException($"The specified '{name}' sms template is not existed.");

			//获取当前电信服务的凭证
			var certificate = this.GetCertificate();

			//生成服务请求的公共头集
			var headers = this.GetCommonHeaders("SendSms");

			//添加必须的短信服务参数
			headers.Add("TemplateCode", template.Code);
			headers.Add("SignName", string.IsNullOrWhiteSpace(scheme) ? template.Scheme : scheme);
			headers.Add("PhoneNumbers", target);

			if(parameter != null)
			{
				if(parameter is string || parameter is System.Text.StringBuilder)
					headers.Add("TemplateParam", parameter.ToString());
				else
					headers.Add("TemplateParam", Serializer.Json.Serialize(parameter, TextSerializationSettings.Camel));
			}

			if(!string.IsNullOrWhiteSpace(extra))
				headers.Add("OutId", extra.Trim());

			//构建短信发送的HTTP请求消息包
			var request = new HttpRequestMessage(HttpMethod.Get, "http://" + PHONE_MESSAGE_DOMAIN + "?" + Utility.GetQueryString(headers));
			request.Headers.Accept.TryParseAdd("application/json");

			//获取当前实例关联的HTTP客户端程序
			var http = this.GetHttpClient(certificate);

			//提交短信发送请求
			var response = await http.SendAsync(request);

			//确认返回状态码是成功的
			response.EnsureSuccessStatusCode();

			return await this.GetResultAsync(response.Content);
		}
		#endregion

		#region 私有方法
		private ICertificate GetCertificate()
		{
			var certificate = _configuration?.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				return Aliyun.Configuration.Instance.Certificates.Default;
			else
				return Aliyun.Configuration.Instance.Certificates.Get(certificate);
		}

		private ServiceCenterName GetRegion()
		{
			return _configuration?.Region ?? Aliyun.Configuration.Instance.Name;
		}

		private HttpClient GetHttpClient(ICertificate certificate)
		{
			return _pool.GetOrAdd(certificate.Name, key =>
			{
				if(certificate == null)
					throw new ArgumentNullException(nameof(certificate));

				var http = new HttpClient(new HttpClientHandler(certificate, PhoneAuthenticator.Instance));

				//尝试构建固定的请求头
				//http.DefaultRequestHeaders.TryAddWithoutValidation("SignatureMethod", "HMAC-SHA1");
				//http.DefaultRequestHeaders.TryAddWithoutValidation("SignatureVersion", "1.0");
				//http.DefaultRequestHeaders.TryAddWithoutValidation("Format", "JSON");
				//http.DefaultRequestHeaders.TryAddWithoutValidation("Action", "SendSms");
				//http.DefaultRequestHeaders.TryAddWithoutValidation("Version", "2017-05-25");

				return http;
			});
		}

		private IDictionary<string, string> GetCommonHeaders(string action)
		{
			return new Dictionary<string, string>
			{
				//操作名称
				{ "Action", action },

				//以下是公共参数部分
				{ "Format", "JSON" },
				{ "Version", "2017-05-25" },
				{ "AccessKeyId", this.GetCertificate().Name },
				{ "SignatureMethod", "HMAC-SHA1" },
				{ "SignatureVersion", "1.0" },
				{ "SignatureNonce", Guid.NewGuid().ToString("N") },
				{ "Timestamp", Utility.GetTimestamp() },
			};
		}

		private async Task<Result> GetResultAsync(HttpContent content)
		{
			var text = await content.ReadAsStringAsync();

			if(string.Equals(content.Headers.ContentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(content.Headers.ContentType.MediaType, "text/json", StringComparison.OrdinalIgnoreCase))
				return Zongsoft.Runtime.Serialization.Serializer.Json.Deserialize<Result>(text);

			return new Result("Unknown", text);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Options.IConfiguration EnsureConfiguration()
		{
			return this.Configuration ?? throw new InvalidOperationException("Missing required configuration of the sms sender(aliyun).");
		}

		private static IDictionary<string, string> GetDictionary(string text)
		{
			if(string.IsNullOrEmpty(text))
				return null;

			var parts = text.Split(',', '|');
			var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			foreach(var part in parts)
			{
				if(string.IsNullOrEmpty(part))
					continue;

				var index = part.IndexOf('=');

				if(index < 0)
					index = part.IndexOf(':');

				if(index <= 0)
					throw new CommandOptionValueException("parameter", text);

				var key = part.Substring(0, index);
				var value = index < part.Length - 1 ? part.Substring(index + 1) : string.Empty;

				dictionary[key] = value;
			}

			return dictionary;
		}
		#endregion

		#region 结果结构
		public struct Result
		{
			public string Code;
			public string Message;

			public Result(string code, string message)
			{
				this.Code = code;
				this.Message = message;
			}

			public override string ToString()
			{
				return "[" + this.Code + "]" + this.Message;
			}
		}
		#endregion

		#region 命令子类
		[CommandOption(KEY_TEMPLATE_OPTION, typeof(string), null, true, "Text.PhoneSendCommand.Options.Template")]
		[CommandOption(KEY_PARAMETER_OPTION, typeof(string), null, false, "Text.PhoneSendCommand.Options.Parameter")]
		[CommandOption(KEY_SCHEME_OPTION, typeof(string), null, false, "Text.PhoneSendCommand.Options.Scheme")]
		[CommandOption(KEY_EXTRA_OPTION, typeof(string), null, false, "Text.PhoneSendCommand.Options.Extra")]
		private class PhoneSendCommand : CommandBase<CommandContext>
		{
			#region 常量定义
			private const string KEY_TEMPLATE_OPTION = "template";
			private const string KEY_PARAMETER_OPTION = "parameter";
			private const string KEY_SCHEME_OPTION = "scheme";
			private const string KEY_EXTRA_OPTION = "extra";
			#endregion

			#region 成员字段
			private Phone _phone;
			#endregion

			#region 构造函数
			internal PhoneSendCommand(Phone phone) : base("Send")
			{
				_phone = phone;
			}
			#endregion

			#region 执行方法
			protected override object OnExecute(CommandContext context)
			{
				if(context.Expression.Arguments == null || context.Expression.Arguments.Length == 0)
					throw new CommandException("Missing arguments.");

				var result = Utility.ExecuteTask(() => _phone.SendAsync(
					context.Expression.Options.GetValue<string>(KEY_TEMPLATE_OPTION),
					context.Expression.Arguments,
					context.Parameter ?? Phone.GetDictionary(context.Expression.Options.GetValue<string>(KEY_PARAMETER_OPTION)),
					context.Expression.Options.GetValue<string>(KEY_SCHEME_OPTION),
					context.Expression.Options.GetValue<string>(KEY_EXTRA_OPTION)));

				return result;
			}
			#endregion
		}

		[CommandOption(KEY_TEMPLATE_OPTION, typeof(string), null, true, "Text.PhoneCallCommand.Options.Template")]
		[CommandOption(KEY_PARAMETER_OPTION, typeof(string), null, false, "Text.PhoneCallCommand.Options.Parameter")]
		[CommandOption(KEY_EXTRA_OPTION, typeof(string), null, false, "Text.PhoneCallCommand.Options.Extra")]
		private class PhoneCallCommand : CommandBase<CommandContext>
		{
			#region 常量定义
			private const string KEY_TEMPLATE_OPTION = "template";
			private const string KEY_PARAMETER_OPTION = "parameter";
			private const string KEY_EXTRA_OPTION = "extra";
			#endregion

			#region 成员字段
			private Phone _phone;
			#endregion

			#region 构造函数
			internal PhoneCallCommand(Phone phone) : base("Call")
			{
				_phone = phone;
			}
			#endregion

			#region 执行方法
			protected override object OnExecute(CommandContext context)
			{
				if(context.Expression.Arguments == null || context.Expression.Arguments.Length == 0)
					throw new CommandException("Missing arguments.");

				return Utility.ExecuteTask(() =>
					this.CallAsync(
						context.Expression.Options.GetValue<string>(KEY_TEMPLATE_OPTION),
						context.Expression.Arguments,
						context.Parameter ?? Phone.GetDictionary(context.Expression.Options.GetValue<string>(KEY_PARAMETER_OPTION)),
						context.Expression.Options.GetValue<string>(KEY_EXTRA_OPTION))
				);
			}

			private async Task<Result[]> CallAsync(string templateCode, string[] phoneNumbers, object parameter, string extra)
			{
				var results = new Result[phoneNumbers.Length];

				for(int i = 0; i < phoneNumbers.Length; i++)
				{
					results[i] = await _phone.CallAsync(templateCode, phoneNumbers[i], parameter, extra);
				}

				return results;
			}
			#endregion
		}
		#endregion
	}
}
