using BenchmarkDotNet.Running;

namespace tests;

class Program
{
    static void Main(string[] args)
    {
        BenchmarkRunner.Run<ProxyTests>();
    }
}
