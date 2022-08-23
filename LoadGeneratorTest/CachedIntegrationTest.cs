using LoadGenerator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace LoadGeneratorTest
{
    [TestClass]

    public class CachedIntegrationTest
    {
        private static Random random = new Random();
        private static int MaxMethodExecutions = 0;

        public void UpdateThreadName()
        {
            if (GlobalVariables.HasUpdatedThreadName) return;
            GlobalVariables.HasUpdatedThreadName = true;
            System.Threading.Thread.CurrentThread.Name = "Main Thread";

        }
        private static int counter = 0;
        private static IEnumerable<TestData> CreateData(ILoadSettings<TestData> settings)
        {
            for(int i=0; i<= MaxMethodExecutions; i++)
                yield return new TestData() { Data = random.Next(1, 1000) + " - abc" + counter++ };
        }


        private static IEnumerable<TestData> CreateDataLongWait(ILoadSettings<TestData> settings)
        {
            for (int i = 0; i <= MaxMethodExecutions; i++)
                yield return new TestData() { Data = random.Next(1, 1000) + " - abc" + counter++, WaitTimeInMs = 5000 };
        }

        private static void WaitWithToken(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            Write(data.Data);
            Task.Delay(data.WaitTimeInMs, threadData.Token).Wait();
        }
        private static void WaitWithoutToken(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            Write(data.Data);
            System.Threading.Thread.Sleep(data.WaitTimeInMs);
        }

        private static void ThrowError(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            throw new Exception("Error thrown");
        }

        private class TestData
        {
            public int WaitTimeInMs { get; set; } = 2000;
            public int WaitTimeInSeconds { get { return WaitTimeInMs / 1000; } }
            public string Data { get; set; } = "abc";

            public override string ToString()
            {
                return Data;
            }
        }

        public static void Write(string s)
        {
            Console.WriteLine($"ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")} - {s}");
        }

        private void LogResults(LoadResults<TestData> results)
        {
            Write("Logging for Debug:");
            Write($"Total Time: {results.TimeSpan}");
            foreach (var r in results.Results)
            {
                Write($"{r.Input} - {r.Success} - {r.Start} - {r.ExecutionTime} - {r?.Error?.Message}");
            }
        }

        private void BasicAsserts(CachedDataLoadSettings<TestData> settings, LoadResults<TestData> results)
        {
            Assert.IsTrue(MaxMethodExecutions <= results.Results.Count(), "Requested number of executions equalts results");
            foreach (var r in results.Results)
            {
                var hasError = r.Error != null;
                Assert.AreEqual(hasError, !r.Success, "Success marked true when it doesn't have an exception");
            }
        }

        //[TestMethod]
        //public void TestsAreNotCancelWithWaitingTokenWhenTimeoutLonger()
        //{
        //    UpdateThreadName();
        //    MaxMethodExecutions = 4;
        //    var settings = new CachedDataLoadSettings<TestData>
        //    {
        //        MaxThreads = 4,
        //        TestDataGenerator = CreateData,
        //        TestMethod = WaitWithToken,
        //        MaxExecutionTimeInSeconds = 3,
        //    };
        //    var d = new CachedDataLoadTesting<TestData>();
        //    var results = d.Execute(settings);

        //    LogResults(results);

        //    Write("Asserts:");
        //    foreach (var r in results.Results)
        //    {
        //        Assert.IsTrue(r.ExecutionTime.TotalSeconds < settings.MaxExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds} < {settings.MaxExecutionTimeInSeconds} - Execution time must be shorter than timeout.");
        //        Assert.IsTrue(r.Error == null, $"No Error: {r.Error}");
        //    }
        //    BasicAsserts(settings, results);

        //}

        [TestMethod]
        public void ThrowErrorDuringLoad()
        {
            UpdateThreadName();
            MaxMethodExecutions = 4;

            var settings = new CachedDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                TestDataGenerator = CreateData,
                TestMethod = ThrowError,
                MaxExecutionTimeInSeconds = 3,
            };

            var results = new CachedDataLoadTesting<TestData>().Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsNotNull(r.Error, "Error");
            }
            BasicAsserts(settings, results);

        }

        [TestMethod]
        public void EndTestExecutionAFterExecutionTimeout()
        {
            UpdateThreadName();
            var settings = new CachedDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 1,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithToken,
                MaxExecutionTimeInSeconds = 1,
            };
            var d = new CachedDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");

            foreach (var r in results.Results)
            {
                var hasError = r.Error != null;
                Assert.AreEqual(hasError, !r.Success, "Success marked true when it doesn't have an exception");
            }

            Assert.IsTrue(results.TimeSpan.TotalSeconds < 15, "Timespan should be much less than 15 seconds as each test takes 5 seconds");
            Assert.IsTrue(results.Results.Count() <= 2, "No more than 2 results should be recorded.");


        }



        //[TestMethod]
        //public void OperationIsCancelledDueToTokenTimeout()
        //{
        //    UpdateThreadName();
        //    MaxMethodExecutions = 10;

        //    var settings = new CachedDataLoadSettings<TestData>
        //    {
        //        MaxThreads = 4,
        //        TestDataGenerator = CreateDataLongWait,
        //        TestMethod = WaitWithToken,
        //        MaxExecutionTimeInSeconds = 1,
        //    };
        //    var d = new CachedDataLoadTesting<TestData>();
        //    var results = d.Execute(settings);

        //    LogResults(results);

        //    Write("Asserts:");
        //    foreach (var r in results.Results)
        //    {
        //        Assert.IsTrue(r.ExecutionTime.TotalSeconds + .01 >= settings.MaxExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds + .01} >= {settings.MaxExecutionTimeInSeconds} - Execution time must be longer than settings");
        //        //Timeout shouldn't be too long.
        //        Assert.IsNotNull(r.Error);
        //        Assert.IsTrue(r.Error.Message.Contains("operation was canceled"), "Error shows operation was canceled");

        //    }
        //    BasicAsserts(settings, results);

        //}

        //[TestMethod]
        //public void PlaceholderResultsUsedWhenNoCancelTokenExists()
        //{
        //    UpdateThreadName();
        //    var settings = new CachedDataLoadSettings<TestData>
        //    {
        //        MaxThreads = 4,
        //        TestMethod = WaitWithoutToken,
        //        MaxExecutionTimeInSeconds = 1,
        //        TestDataGenerator = CreateDataLongWait,
        //    };

        //    var results = new CachedDataLoadTesting<TestData>().Execute(settings);

        //    LogResults(results);

        //    Write("Asserts:");
        //    foreach (var r in results.Results)
        //    {
        //        Assert.IsTrue(r.ExecutionTime.TotalSeconds + .01 >= settings.MaxExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds + .01} >= {settings.MaxExecutionTimeInSeconds} - Execution time must be longer than settings");
        //        Assert.IsNotNull(r.Error);
        //        Assert.IsTrue(r.Error.Message.Contains("Test timed out"), "Error shows test timed out.");

        //    }
        //    BasicAsserts(settings, results);

        //}

    }
}
