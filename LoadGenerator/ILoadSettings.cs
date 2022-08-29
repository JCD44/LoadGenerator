using System;

namespace LoadGenerator
{
    public interface ILoadSettings<TestData>
    {
        Action<ILoadSettings<TestData>, ThreadSupportData, TestData> TestMethod { get; set; }
        int MaxSimulatedUsers { get; set; }
        //Timeouts do not kill the process.  Instead they will request the thread ends and failing that they simply
        //let the thread run on forever.  This is not ideal design, but the alternative is to run a separate process for each
        //call and that takes too long for some load testing. - https://docs.microsoft.com/en-us/dotnet/standard/threading/destroying-threads
        int MaxExecutionTimeInSeconds { get; set; }


    }
}
