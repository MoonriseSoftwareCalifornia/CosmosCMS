namespace Cosmos.MultiTenant_Adminstrator.Controllers
{
    internal class TestResult
    {
        public TestResult()
        {
        }

        public bool IsDatabaseConnected { get; internal set; }
        public bool IsStorageConnected { get; internal set; }
        public string ErrorMessage { get; internal set; }
    }
}