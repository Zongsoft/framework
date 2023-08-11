using System;

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
#if NET7_0
			var app = Application.Web("Zongsoft.Web", args);
			app.Map("/", async ctx => ctx.Response.Redirect("/Application"));
			app.Run();
#else
			var app = Application.Web("Zongsoft.Web", args, (_, app) =>
				app.UseEndpoints(endpoints =>
					endpoints.Map("/", async ctx => ctx.Response.Redirect("/Application"))
				)
			);
			app.Run();
#endif
		}
	}
}
