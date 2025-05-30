using System;

using BenchmarkDotNet;
using BenchmarkDotNet.Running;

namespace Zongsoft.Benchmarks;

internal class Program
{
	static void Main(string[] args)
	{
		BenchmarkRunner.Run(typeof(Program).Assembly);
	}
}
