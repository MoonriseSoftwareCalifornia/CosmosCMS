using Cosmos.Common.Services.PowerBI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Cosmos.Tests
{
    [TestClass]
    public class AppOwnsDataTests
    {
        [TestMethod]
        public void AcquirePowerBiTokenForClient_Test()
        {
            var utilities = new TestUtilities();

            var config = utilities.GetConfig();
            var pbi = config.GetSection("PowerBiAuth").Get<PowerBiAuth>();

            var tokenService = new PowerBiTokenService(Options.Create(pbi));

            var token = tokenService.GetAppAccessToken().Result;
            
            Assert.IsNotNull(token);
        }

        [TestMethod]
        public void GetEmbedParams_Test()
        {
            var utilities = new TestUtilities();

            var config = utilities.GetConfig();
            var pbi = config.GetSection("PowerBiAuth").Get<PowerBiAuth>();

            var tokenService = new PowerBiTokenService(Options.Create(pbi));
            var result = tokenService.GetEmbedParams(Guid.Parse("2012c9d4-180b-4a79-965b-6c078c6fdae1"), Guid.Parse("4985ca57-f1c9-48c5-99ae-8308c8f2e534")).Result;

            var t = result;
        }
    }
}