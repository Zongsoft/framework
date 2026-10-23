using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying;

public static partial class HttpUtility
{
	internal static OperationException GetFailure(HttpResponseMessage response, byte[] body, bool verified)
	{
		var code = "HTTP_" + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
		string message = null;

		if(verified && body.Length > 0)
		{
			try
			{
				using var document = JsonDocument.Parse(body);
				if(document.RootElement.ValueKind == JsonValueKind.Object)
				{
					if(document.RootElement.TryGetProperty("code", out var property) && property.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(property.GetString()))
						code = property.GetString();
					if(document.RootElement.TryGetProperty("message", out property) && property.ValueKind == JsonValueKind.String)
						message = property.GetString();
				}
			}
			catch(JsonException)
			{
				//非 JSON 错误只保留 HTTP 状态，不将网关正文写入异常。
			}
		}

		var exception = new OperationException(code, message);
		exception.Data["ResponseVerified"] = verified;
		SetFailureContext(exception, response);
		return exception;
	}

	internal static void SetFailureContext(Exception exception, HttpResponseMessage response)
	{
		exception.Data["StatusCode"] = (int)response.StatusCode;
		if(!exception.Data.Contains("ResponseVerified"))
			exception.Data["ResponseVerified"] = response.Content is VerifiedContent;

		if(response.Headers.TryGetValues("Request-ID", out var values))
		{
			var identifiers = values.ToArray();
			if(identifiers.Length == 1 && identifiers[0].Length <= 256 && !identifiers[0].Any(char.IsControl))
				exception.Data["RequestId"] = identifiers[0];
		}
	}

	internal sealed class VerifiedContent(byte[] body) : ByteArrayContent(body) { }
}
