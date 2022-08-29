namespace LoadGenerator.Events
{
    /// <summary>
    /// If settings are updated it will run on the main thread, meaning it may impact performance.  Only set 
    /// this to true if "Execute" method is intended to update settings.
    /// </summary>
    public interface ISettingsUpdate
    {

    }
}
