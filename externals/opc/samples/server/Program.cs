using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal static class Program
{
	static async Task Main(string[] args)
	{
		using var server = new OpcServer("OpcServer");
		server.Options.Prefabs.Define();
		await server.StartAsync(args);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync(args, cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync(args, cancellation));

		await executor.RunAsync($"Welcome to the OPC-UA Server.{Environment.NewLine}{new string('-', 50)}");
	}

	private static void Define(this PrefabCollection prefabs)
	{
		prefabs.Folder("F0", "Folder #0");
		prefabs.Folder("F1", "Folder #1")
			.Variable<float>("x");
		prefabs.Folder("F2", "Folder #2")
			.Folder("F2-A", "Folder A")
			.Variable<ulong>("y", ulong.MaxValue);
		prefabs.Folder("F3", "Folder #3")
			.Folder("F3-A", "Folder A")
			.Folder("F3-A-1")
			.Variable<byte[]>("z");

		var variables = prefabs.Folder("variables");
		variables.Variable("double", typeof(double));
		variables.Variable("float", typeof(float), 1.23);
		variables.Variable("integer", typeof(int), 100);
		variables.Variable<string>("text");
		variables.Variable("boolean", true);
		variables.Variable("guid", Guid.NewGuid());
		variables.Variable("date", DateTime.Now);
		variables.Variable<byte[]>("binary", [1, 2, 3, 4, 5]);
	}

	public class Person
	{
		public string Name { get; set; }
		public bool? Gender { get; set; }
		public DateTime? Birthday { get; set; }
		public Address? HomeAddress { get; set; }
		public Address? OfficeAddress { get; set; }
	}

	public struct Address
	{
		public int Country { get; set; }
		public string Province { get; set; }
		public string City { get; set; }
		public string Street { get; set; }
		public string Detail { get; set; }
		public string PostalCode { get; set; }
	}
}
