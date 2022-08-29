using LoadGenerator.Results;

namespace LoadGenerator
{
    public interface ILoadTesting<TestData>
    {
        public ILoadResults<TestData> Execute(ILoadSettings<TestData> settings);
    }
}
