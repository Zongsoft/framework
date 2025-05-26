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
		server.Options.Storages.Define("http://zongsoft.com/opc/ua").Initialize();
		await server.StartAsync(args);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync(args, cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync(args, cancellation));

		executor.Command("get", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.IsEmpty())
				return;

			if(context.Expression.Arguments.Length == 1)
			{
				var value = await server.GetValueAsync(context.Expression.Arguments[0], cancellation);
				var content = CommandOutletContent.Create(string.Empty).AppendValue(value);
				context.Output.Write(content);
			}
			else
			{
				var result = server.GetValuesAsync(context.Expression.Arguments, cancellation);

				await foreach(var entry in result)
				{
					var content = CommandOutletContent
						.Create(CommandOutletColor.DarkYellow, entry.Key)
						.Append(CommandOutletColor.DarkGray, " : ")
						.AppendValue(entry.Value);

					context.Output.Write(content);
				}
			}
		});

		executor.Command("set", async (context, cancellation) =>
		{
			if(context.Expression.Arguments.Length < 2)
				throw new CommandException($"Missing required argument of the command.");

			//获取指定键对应的数据类型
			var type = await server.GetDataTypeAsync(context.Expression.Arguments[0], cancellation) ??
				throw new CommandException($"The specified '{context.Expression.Arguments[0]}' does not exist, or its data type is not available.");

			object value = null;

			if(type.IsArray)
			{
				type = type.GetElementType();
				value = Array.CreateInstance(type, context.Expression.Arguments.Length - 1);

				for(int i = 1; i < context.Expression.Arguments.Length; i++)
				{
					((Array)value).SetValue(Common.Convert.ConvertValue(context.Expression.Arguments[i], type), i - 1);
				}
			}
			else
			{
				if(context.Expression.Arguments.Length > 2)
					throw new CommandException($"Too many command arguments.");

				value = Common.Convert.ConvertValue(context.Expression.Arguments[1], type);
			}

			var succeed = await server.SetValueAsync(context.Expression.Arguments[0], value, cancellation);

			if(succeed)
				context.Output.WriteLine(CommandOutletColor.DarkGreen, "The set operation was successful.");
			else
				context.Output.WriteLine(CommandOutletColor.DarkRed, "The set operation failed.");
		});

		await executor.RunAsync($"Welcome to the OPC-UA Server.{Environment.NewLine}{new string('-', 50)}");
	}

	private static void Initialize(this OpcServerOptions.StorageOptions storage) => Initialize(storage.Prefabs);
	private static void Initialize(this PrefabCollection prefabs)
	{
		if(prefabs == null)
			return;

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
