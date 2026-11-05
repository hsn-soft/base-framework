using BenchmarkDotNet.Running;

// The hot paths of the framework, measured: run a group with  dotnet run -c Release --project benchmarks/HsnSoft.Base.Benchmarks -- --filter "*Masking*"
// (or all of them with --filter "*"). Nothing here is part of the tests or of the CI: a number is read, then compared with the one of the change before it.
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
