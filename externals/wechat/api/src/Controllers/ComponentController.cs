using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Zongsoft.Externals.Wechat.Platforms;

namespace Zongsoft.Externals.Wechat.Web.Controllers;

[Area("Externals/Wechat")]
[Zongsoft.Web.ControllerName("Components")]
public class ComponentController : ControllerBase
{
	[HttpPost("{id}/Authorizations")]
	[RequestSizeLimit(4096)]
	public async ValueTask<IActionResult> AuthorizeAsync(string id, [FromBody] AuthorizationRequest request, CancellationToken cancellation = default)
	{
		if(this.User.Identity?.IsAuthenticated != true)
			return this.StatusCode(StatusCodes.Status403Forbidden);

		var owner = this.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

		if(string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(id) || request == null || string.IsNullOrWhiteSpace(request.Context) ||
			request.AccountType is not (AccountType.Channel or AccountType.Applet) || !Uri.TryCreate(request.RedirectUri, UriKind.Absolute, out var redirect) || redirect.Scheme != "https")
			return this.BadRequest();
		if(!await WebUtility.AuthorizeAsync(this.HttpContext, new AuthorizationContext(id, request.Context, redirect, request.AccountType), "Wechat.Components", cancellation))
			return this.StatusCode(StatusCodes.Status403Forbidden);

		var component = ComponentManager.Find(id);

		if(component == null)
			return this.NotFound();

		try
		{
			var result = await component.BeginAuthorizationAsync(owner, request.Context, redirect, request.AccountType, request.Mobile, cancellation);
			return this.Ok(new { Url = result.AuthorizationUrl, result.Expiration });
		}
		catch(ArgumentException)
		{
			return this.BadRequest();
		}
		catch(Common.OperationException)
		{
			return this.StatusCode(StatusCodes.Status503ServiceUnavailable);
		}
	}

	public readonly record struct AuthorizationContext(string ComponentId, string Context, Uri RedirectUri, AccountType AccountType);

	public sealed class AuthorizationRequest
	{
		public string Context { get; set; }
		public string RedirectUri { get; set; }
		public AccountType AccountType { get; set; }
		public bool Mobile { get; set; }
	}
}
