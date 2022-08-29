namespace LoadGenerator.Events
{
    public class ConsoleLoggingEvent<TestData> : AbstractLoggingEvent<TestData>
    {
        public override void Log(string s)
        {
            System.Console.WriteLine(s);
        }
    }
}
