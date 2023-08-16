using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Zongsoft.Web;

namespace Zongsoft.Hosting.Web
{
	internal class Program
	{
		public static void Main(string[] args)
		{
#if NET7_0_OR_GREATER
			var app = Application.Web(args);
			app.Map("/", ctx => { ctx.Response.Redirect("/Application"); return Task.CompletedTask; });
			app.Run();
#else
			var app = Application.Web(args, (_, app) =>
				app.UseEndpoints(endpoints =>
					endpoints.Map("/", ctx => { ctx.Response.Redirect("/Application"); return Task.CompletedTask; })
				)
			);
			app.Run();
#endif
		}
	}
}
