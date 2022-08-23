using LoadGenerator;
using LoadGenerator.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGeneratorTest
{
    [TestClass]
    public class DynamicIntegrationTests
    {
        private static Random random = new Random();

        public void UpdateThreadName()
        {
            if (GlobalVariables.HasUpdatedThreadName) return;
            GlobalVariables.HasUpdatedThreadName = true;
            System.Threading.Thread.CurrentThread.Name = "Main Thread";

        }
        private static int counter = 0;
        private static TestData CreateData(ILoadSettings<TestData> settings)
        {
            return new TestData() { Data = random.Next(1, 1000) + " - abc" + counter++ };
        }

        private static TestData CreateDataLongWait(ILoadSettings<TestData> settings)
        {
            return new TestData() { Data = random.Next(1, 1000) + " - abc" + counter++, WaitTimeInMs = 5000 };
        }

        public static void RunTest(ILoadSettings<string> settings, string data)
        {
            Write(data);
            System.Threading.Thread.Sleep(1000);
        }
        private static void WaitWithToken(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            Write(data.Data);
            threadData.Task.Wait(data.WaitTimeInMs, threadData.Token);
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

        private void BasicAsserts(DynamicDataLoadSettings<TestData> settings, LoadResults<TestData> results)
        {
            Assert.IsTrue(settings.MaxMethodExecutions <= results.Results.Count(), "Requested number of executions equals results");
            foreach (var r in results.Results) 
            {
                var hasError = r.Error != null;
                Assert.AreEqual(hasError, !r.Success, "Success marked true when it doesn't have an exception");
            }
        }

        [TestMethod]
        public void TestsAreNotCancelWithWaitingTokenWhenTimeoutLonger()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 4,
                TestDataGenerator = CreateData,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 3,
            };
            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.ExecutionTime.TotalSeconds < settings.MaxTestExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds} < {settings.MaxTestExecutionTimeInSeconds} - Execution time must be shorter than timeout.");
                Assert.IsTrue(r.Error==null, $"No Error: {r.Error}");
            }
            BasicAsserts(settings, results);

        }

        private class ConsoleLoggingEventCounter<TestData> : ConsoleLoggingEvent<TestData>
        {
            public int Counter { get; set; }
            public override void Log(string s)
            {
                Counter++;
                System.Console.WriteLine(s);
            }

        }

        [TestMethod]
        public void EventsAreExecutedCorrectNumberOfTimes()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 50,
                MaxMethodExecutions = 100,
                TestDataGenerator = CreateData,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 3,
                EventFrequencyInSeconds = 0
            };
            var logging = new ConsoleLoggingEventCounter<TestData>()
            {
                TimeBetweenCalls = TimeSpan.FromSeconds(1),
            };
            settings.Events.Add(logging);

            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            
            BasicAsserts(settings, results);
            Assert.IsTrue(logging.Counter > 0, $"{logging.Counter} > 0");

        }

        //private Task CreateTask()
        //{
        //    var cts = new CancellationTokenSource();
        //    cts.CancelAfter(3 * 1000);
        //    var token = cts.Token;
        //    //var threadData = new ThreadSupportData(cts);
        //    //Action<ILoadSettings<TestData>, ThreadSupportData, TestData> TestMethod = WaitWithToken;
        //    Action action = () =>
        //    {
        //        Write("Starting task");

        //        //while (threadData.Task == null) { Thread.Sleep(1); }

        //        Thread t = Thread.CurrentThread;
        //        //threadData.Token.Register(() => { System.Console.WriteLine($"{t.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")} - Cancel requested"); });
        //        token.Register(() => { System.Console.WriteLine($"{t.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")} - Cancel requested"); });
        //        var data = CreateData(null);
        //        Write(data.Data);
        //        //Task.Delay(data.WaitTimeInMs, threadData.Token).Wait();
        //        Task.Delay(data.WaitTimeInMs, token).Wait();
        //        //threadData.Task.Wait(data.WaitTimeInMs, threadData.Token);
        //        Write("Done!");

        //    };
        //    var t = Task.Run(action, token);

        //    //var t = Task.Run(action, threadData.Token);
        //    //threadData.Task = t;

        //    return t;

        //}

        //private Task CreateTask(int taskNumber)
        //{
        //    var cts = new CancellationTokenSource();
        //    var msTimeOut = 3 * 1000;
        //    var format = "hh:mm:ss.fff";
        //    cts.CancelAfter(msTimeOut);
        //    var token = cts.Token;
        //    var time = DateTime.Now.AddMilliseconds(msTimeOut);
        //    Action action = () =>
        //    {
        //        Write($"Starting task {taskNumber}");

        //        token.Register(() => { Write($"Cancel requested {taskNumber} - Cancel expected after ~{time.ToString(format)}"); });
        //        Write($"Delaying Task #: {taskNumber}");
        //        Task.Delay(2000, token).Wait();
        //        Write($"Done {taskNumber}!");

        //    };
        //    Write($"Task Queued {taskNumber}");
        //    return Task.Run(action, token);
        //}

        //[TestMethod]
        //public void PoCBug()
        //{
        //    UpdateThreadName();

        //    var l = new List<Task>();
        //    for (int i = 0; i != 5; i++) {
        //        l.Add(CreateTask(i));
        //        System.Threading.Thread.Sleep(50);
        //    }

        //    //Causes a task cancelled exception:
        //    //Task.WaitAll(l.ToArray(), 5000);
        //    while (l.Count(a => a.IsCompleted || a.IsFaulted || a.IsCanceled) != l.Count)
        //    {
        //        System.Threading.Thread.Sleep(50);
        //    }

        //    var cancelled = l.Count(a => a.IsCanceled || a.IsFaulted);
        //    Write($"Total Cancelled: {cancelled}");
        //    Assert.IsTrue(cancelled == 0);


        //}


        [TestMethod]
        public void ThrowErrorDuringLoad()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 4,
                TestDataGenerator = CreateData,
                TestMethod = ThrowError,
                MaxTestExecutionTimeInSeconds = 3,
            };

            var results = new DynamicDataLoadTesting<TestData>().Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.Error != null, "Error");
            }
            BasicAsserts(settings, results);

        }



        [TestMethod]
        public void OperationIsCancelledDueToTokenTimeout()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 10,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 1,
            };
            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.ExecutionTime.TotalSeconds + .01 >= settings.MaxTestExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds + .01} >= {settings.MaxTestExecutionTimeInSeconds} - Execution time must be longer than settings");
                //Timeout shouldn't be too long.
                Assert.IsNotNull(r.Error);
                Assert.IsTrue(r.Error.Message.Contains("operation was canceled"), "Error shows operation was canceled");

            }
            BasicAsserts(settings, results);

        }

        [TestMethod]
        public void EndTestExecutionAFterExecutionTimeout()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 1,
                MaxMethodExecutions = 100,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithToken,
                MaxExecutionTimeInSeconds = 1,
            };
            var d = new DynamicDataLoadTesting<TestData>();
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

        [TestMethod]
        public void PlaceholderResultsUsedWhenNoCancelTokenExists()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 4,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithoutToken,
                MaxTestExecutionTimeInSeconds = 1,
            };

            var results = new DynamicDataLoadTesting<TestData>().Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.ExecutionTime.TotalSeconds + .01 >= settings.MaxTestExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds + .01} >= {settings.MaxTestExecutionTimeInSeconds} - Execution time must be longer than settings");
                Assert.IsTrue(r.Error.Message.Contains("Test timed out"), "Error shows test timed out.");

            }
            BasicAsserts(settings, results);

        }

    }
}
