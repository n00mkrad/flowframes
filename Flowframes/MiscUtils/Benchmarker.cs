using System;

namespace Flowframes.MiscUtils
{
    class Benchmarker
    {
        // Benchmark a method with return type (via Delegate/Func)
        public static object BenchmarkMethod(string methodName, Delegate method, params object[] args)
        {
            NmkdStopwatch sw = new NmkdStopwatch();
            var returnVal =  method.DynamicInvoke(args);
            Logger.Log($"Ran {methodName} in {sw}", true);
            return returnVal;
        }

        // Benchmark a void method (via Action)
        public static void BenchmarkMethod(string methodName, Action method, params object[] args)
        {
            NmkdStopwatch sw = new NmkdStopwatch();
            method.DynamicInvoke(args);
            Logger.Log($"Ran {methodName} in {sw}", true);
        }
    }
}
