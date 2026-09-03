using System;

using BenchmarkDotNet.Running;

namespace Zongsoft.Benchmarks;

internal class Program
{
	static void Main(string[] args)
	{
		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}
