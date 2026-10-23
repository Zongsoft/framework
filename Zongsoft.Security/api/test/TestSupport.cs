using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Security.Privileges;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Zongsoft.Security.Web.Tests;

public class ServiceProxy : DispatchProxy
{
	public Func<MethodInfo, object[], object> Handler { get; set; }

	public static T Create<T>(Func<MethodInfo, object[], object> handler) where T : class
	{
		var service = Create<T, ServiceProxy>();
		((ServiceProxy)(object)service).Handler = handler;
		return service;
	}

	protected override object Invoke(MethodInfo targetMethod, object[] args) => this.Handler(targetMethod, args);
}

internal static class TestContext
{
	public static T Attach<T>(T controller, string body = null, string contentType = "text/plain", IServiceProvider services = null) where T : ControllerBase
	{
		var bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);
		controller.ControllerContext = new ControllerContext
		{
			HttpContext = new DefaultHttpContext
			{
				RequestServices = services ?? new ServiceCollection().BuildServiceProvider(),
			},
		};
		controller.Request.Body = new MemoryStream(bytes);
		controller.Request.ContentLength = bytes.Length;
		controller.Request.ContentType = contentType;
		return controller;
	}

	public static async IAsyncEnumerable<T> Sequence<T>(params T[] items)
	{
		foreach(var item in items)
			yield return item;
		await Task.CompletedTask;
	}
}

internal sealed class SecurityState : IDisposable
{
	private static readonly FieldInfo _authorityField = typeof(Authentication).GetField("_authority", BindingFlags.Static | BindingFlags.NonPublic);
	private readonly object _authority = _authorityField.GetValue(null);
	private readonly IClaimsPrincipalTransformer _transformer = Authentication.Transformer;
	private readonly IAuthenticator[] _authenticators = [.. Authentication.Authenticators];
	private readonly IChallenger[] _challengers = [.. Authentication.Challengers];
	private readonly IAuthorizer _authorizer = Authorization.Authorizer;
	private readonly IAuthorizer[] _authorizers = [.. Authorization.Authorizers];

	public void Dispose()
	{
		// Authority rejects null in its public setter, so preserve the original uninitialized state exactly.
		_authorityField.SetValue(null, _authority);
		Authentication.Transformer = _transformer;
		Authentication.Authenticators.Clear();

		foreach(var authenticator in _authenticators)
			Authentication.Authenticators.Add(authenticator);

		Authentication.Challengers.Clear();

		foreach(var challenger in _challengers)
			Authentication.Challengers.Add(challenger);

		Authorization.Authorizers.Clear();

		foreach(var authorizer in _authorizers)
			Authorization.Authorizers.Add(authorizer);

		Authorization.Authorizer = _authorizer;
	}
}
