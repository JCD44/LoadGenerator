# LoadGenerator

A package designed to allow you to run load tests on any arbitrary method that conforms to a specific signature.

## Who is this for?

This is for engineers who make integration tests using API calls.  Unfortunately, those integration tests never include any load testing.  Instead, we make big complex load tests using record and playback of our website, making it difficult to debug which elements of the site are slow.  Furthermore, because the load test is complex and often not built in the same tool we do everything else in, we don't run it frequently, so we don't get fast feedback on changes to performance.  This tool is meant to allow you to do smaller load tests more frequently, mostly at the API level.  It is not meant to replace the load testing tools which are more robust and complex, but rather to supplement them in the same way unit tests are not meant to replace system level testing.  The big advantage to this system is that it is easy to wrap an API call in the framework and then perform a load test for each and every API endpoint you have.  If you're not sure what the expected performance should be for a given endpoint in your non-production environment, you can even record the performance data into a database and then use that as a baseline.

## Features

* Customizable load testing
  - You can run load on a preloaded list of datapoints
  - You can run load on a dynamically generated dataset
  - You can run load with no dynamic data
* Dynamic Reporting
  - You can output data via events during the load test.
* Dynamic load
  - You can dynamically adjust the load test using events.
* Built on top of TPL
  - You can use task cancel tokens to dynamically timeout or end tests early.
 
# Usage

Two things are needed to run a load test.  Settings and something to execute those settings.  It is recommended you use the dynamic data load test for all features, while the cached data load test system has many few features.  The cached system is primarily just a Parallel.ForEach under the hood, while the dynamic data manages tasks directly.  Due to this difference, the dynamic data runs with slightly more overhead, but has many additional features.  Here are the primary settings for the dynamic data load test:

* MaxSimulatedUsers  - In some load test systems these would be referred to as threads.  This will directly impact the TPL thread pool.
* MaxMethodExecutions  - The maximum number of times a method will be called.
* TestDataGenerator  - The method called in order to generate data for the test method.  This maybe be something like user credentials, url data, etc.
* TestMethod  - The method which will run the test.  This method must take in some specific parameters.
* MaxTestExecutionTimeInSeconds  - The maximum time load will be generated.  In the cached system, if this time is exceeded it will request the for loop cancel.  In the dynamic system, if time is exceeded, it will allow running tasks to finish up to the MaxExecutionTimeInSeconds before requesting the methods be cancelled.
* MaxExecutionTimeInSeconds  - The maximum time a single method call will run before the task is requested to be cancelled.
* Events  - Events have two parts, "should I run?" and "what I do".  
  - The "should I run?" piece needs to run quickly as it runs on the main thread.
  - You should only create two styles of events, "reporting" or read only events and "settings" or write only events.  It is strongly recommended not to mix the two.
  - If an event should be run and it does not update settings, it will be run on a separate task.  If it will update settings it must run on the main thread.
  - Events are always run after the load test is completed, regardless of the result from "should I run?"
* EventFrequencyInSeconds  - How frequently the events have the "should I run?" piece executed.  Set to 0 means everytime the system has enough "max" tasks running, events will be checked.


## Example Usage:

Here is an example of creating settings in which the input object is a simple string, and there is an event which logs to the console:


```
            var settings = new DynamicDataLoadSettings<String>
            {
                MaxSimulatedUsers = 50,
                MaxMethodExecutions = 100,
                TestDataGenerator = (TestData) => {
                    return "abc"; 
                },
                TestMethod = (ILoadSettings<string> settings, ThreadSupportData thread, string data)=> { 
                    System.Console.WriteLine($"Do stuff with {data}"); 
                },
                MaxTestExecutionTimeInSeconds = 60,
                MaxExecutionTimeInSeconds = 500,
                EventFrequencyInSeconds = 1
            };
            var logging = new ConsoleLoggingEvent<string>()
            {
                TimeBetweenCalls = TimeSpan.FromSeconds(1),
            };
            settings.Events.Add(logging);

            var loadTest = new DynamicDataLoadTesting<string>();
            var results = loadTest.Execute(settings);
```

Here is the same code but with a complex object called "TestData":

```
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 50,
                MaxMethodExecutions = 100,
                TestDataGenerator = (TestData) => {
                    return new TestData() { Data = "abc" }; 
                },
                TestMethod = (ILoadSettings<TestData> settings, ThreadSupportData thread, TestData data)=> { 
                    System.Console.WriteLine("Do stuff with data"); 
                },
                MaxTestExecutionTimeInSeconds = 60,
                MaxExecutionTimeInSeconds = 500,
                EventFrequencyInSeconds = 1
            };
            var logging = new ConsoleLoggingEvent<TestData>()
            {
                TimeBetweenCalls = TimeSpan.FromSeconds(1),
            };
            settings.Events.Add(logging);
            var loadTest = new DynamicDataLoadTesting<TestData>();
            var results = loadTest.Execute(settings);
```

Here is an example of building a custom event for logging:

```
        private class ConsoleLoggingEventCounter<TestData> : ConsoleLoggingEvent<TestData>
        {
            public override void Log(string s)
            {
                WriteLineToDatabase(s);
            }

        }
```


Here is an example of building a custom event for updating settings:

```
 private class AdjustSettingsEvent<TestData> : AbstractTimeBasedEvent<TestData>, ISettingsUpdate//You must implement ISettingsUpdate in order to update settings.
        {
            public override ILoadSettings<TestData> Execute(ILoadResults<TestData> results, ILoadSettings<TestData> settings)
            {
                var ts = results.EndTime - results.StartTime;

                var resultCount = results.TotalResults;
                var failures = results.TotalFailures;
                var tps = (resultCount - failures) / ts.TotalSeconds;

                if(tps<=20 && settings.MaxSimulatedUsers<50) settings.MaxSimulatedUsers += 5;
                Write("Updating settings");

                return settings;
            }
        }
```

### Unexpected Complexity
TPL is designed to dynamically adjust the number of tasks or threads it can run at once.  As you use TPL it may add additional threads to the thread pool, but that requires time for the system to respond.  Time we don't have.  To deal with this, we have to update the threadpool with the number of users your load test requests.  Yet, you may use tasks in your test method or any third party libraries may use tasks.  How many threads do we really need?  Unfortunately, it is impossible to know, so as a stab in the dark, we take max users and multiply it by a fudge factor + n additional threads.  These values can be changed by you in the load test instance like this:
```
            var loadTest = new DynamicDataLoadTesting<TestData>();
            //40% additional overhead
            loadTest.TaskOverheadPercentage = .4;
            //+10 more fixed tasks
            loadTest.TaskOverheadFixed = 10;
            ...
            var results = loadTest.Execute(settings);
```


## The Code

The code can give you insights into how the program works.  In particular not every value is outputted and sometimes you want to get a deeper look into what the application is doing.  Fortunately for you, it is all open source and should be as easy as opening the project in visual studio.

### Prerequisites

There are NO nuget packages this depends on at present.


### Installing

At present no nuget exists for this, so you will have to create your own.


## Running the tests

There are no known special requirements to run the tests.

### Break down the testing

Currently only some basic integration tests exist.  One interesting problem is that many of the possible bugs are threading specific, making it nearly impossible to shake out via single runs.  I've tried running the tests multiple times and they seem solid.


## Built With

* Visual Studio 2022
* .net standard 2.1

## Contributing

Feel free to contact me, however any pull request without clear documentation of intent, including tests, will be rejected.

## Versioning

0.9.1 - Added summary result data used for reporting.  Some breaking changes occurred in results where names were changed.  Did some cleanup based upon .net analysis after having upgraded to VS 2022.
0.9.0 - Initial Release.


## Primary Authors

* **JCD**  - *Initial work*


## License

This project is licensed under the GPL v2 License  - see the [LICENSE](LICENSE) file for details

## Acknowledgments




### Fine Print
The makers of this application make no representations as to accuracy, completeness, currentness, suitability, or validity of any information generated by this application and will not be liable for any errors, omissions, or delays in this information or any losses, injuries, or damages arising from its display or use. Furthermore, the makers of this application assume no responsibility for the accuracy, completeness, currentness, suitability, and validity of any and all external links you find here. The makers of the application assume no responsibility or liability for postings and pull requests by users.
