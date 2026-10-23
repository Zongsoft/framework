using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Zongsoft.Externals.Wechat.Platforms;

namespace Zongsoft.Externals.Wechat.Gateway.Controllers;

[AllowAnonymous]
[Area("Externals/Wechat")]
[Zongsoft.Web.ControllerName("Components")]
[RequestSizeLimit(256 * 1024)]
public class ComponentController : ControllerBase
{
	[HttpPost("{id}/Events")]
	public async ValueTask<IActionResult> ReceiveAsync(string id, CancellationToken cancellation = default)
	{
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		timeout.CancelAfter(TimeSpan.FromSeconds(4));

		try
		{
			var query = this.Request.Query;

			if(!Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(this.Request.ContentType, out var contentType) ||
				!(contentType.MediaType.Equals("text/xml", StringComparison.OrdinalIgnoreCase) || contentType.MediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase)) ||
				query.Any(item => item.Value.Count != 1 || item.Key is not ("timestamp" or "nonce" or "msg_signature" or "signature" or "encrypt_type" or "openid")) ||
				(query.ContainsKey("encrypt_type") && query["encrypt_type"] != "aes"))
				return this.BadRequest();

			var component = ComponentManager.Find(id);

			if(component == null)
				return this.NotFound();

			var receipt = await component.ReceiveAsync(this.Request.Body, query["timestamp"], query["nonce"], query["msg_signature"], timeout.Token);
			return receipt is Component.Receipt.Accepted or Component.Receipt.Duplicate ? this.Content("success", "text/plain") : this.StatusCode(StatusCodes.Status503ServiceUnavailable);
		}
		catch(Exception error) when(error is CryptographicException or System.Xml.XmlException or FormatException or ArgumentException or OverflowException)
		{
			return this.BadRequest();
		}
		catch(Exception)
		{
			return this.StatusCode(StatusCodes.Status503ServiceUnavailable);
		}
	}
}
