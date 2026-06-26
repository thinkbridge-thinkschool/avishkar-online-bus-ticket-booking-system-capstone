using BenchmarkDotNet.Running;
using BusBooking.Benchmarks;

BenchmarkRunner.Run<SearchSchedulesBenchmark>(args: args);
