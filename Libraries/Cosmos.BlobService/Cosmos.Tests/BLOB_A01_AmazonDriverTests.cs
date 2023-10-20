using Cosmos.BlobService;
using Cosmos.BlobService.Config;
using Cosmos.BlobService.Drivers;
using Cosmos.BlobService.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class BLOB_A01_AmazonDriverTests
    {
        private static CosmosStorageConfig? _cosmosConfig;

        private static string? _fullPathTestFile;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var config = new CosmosStorageConfig
            {
                StorageConfig = new StorageConfig()
            };

            var cosmosConfig = ConfigUtilities.GetCosmosConfig();

            config.StorageConfig = cosmosConfig.Value.StorageConfig;

            _cosmosConfig = config;

            _fullPathTestFile = Path.Combine(context.DeploymentDirectory, BLOB_Driver_TestConstants.TestFile1);
        }

        [TestMethod]
        public async Task A01_GetList()
        {
            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            var blobs = await driver.GetObjectsAsync("", null);

            Assert.IsNotNull(blobs);
        }

        [TestMethod]
        public async Task A02_DeleteItems()
        {
            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            // Delete all blobs
            var blobs = await driver.DeleteFolderAsync("");

            // See if they are deleted
            var results = await driver.GetObjectsAsync("", null);

            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public async Task A03_CreateFolderSuccess()
        {
            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            await driver.CreateFolderAsync(BLOB_Driver_TestConstants.FolderHelloWorld1);
        }

        [TestMethod]
        public async Task A04_CreateSubFolders()
        {
            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            await driver.CreateFolderAsync(BLOB_Driver_TestConstants.FolderHelloWorld1);

            await driver.CreateFolderAsync(BLOB_Driver_TestConstants.HelloWorld1SubDirectory1);

            await driver.CreateFolderAsync(BLOB_Driver_TestConstants.HelloWorldSubDirectory2);

            await driver.CreateFolderAsync(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory3);

            // Get all blobs
            var blobs = await driver.GetObjectsAsync("", null);
            Assert.AreEqual(4, blobs.Count);
        }

        [TestMethod]
        public async Task A05_GetSubFolders()
        {
            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            // Get all blobs
            var blobs = await driver.GetObjectsAsync("/hello-world-1/", null);

            Assert.AreEqual(4, blobs.Count);

            var subBlobs1 = await driver.GetObjectsAsync(BLOB_Driver_TestConstants.HelloWorld1SubDirectory1, null);
            Assert.AreEqual(1, subBlobs1.Count);

            var subBlobs2 = await driver.GetObjectsAsync(BLOB_Driver_TestConstants.HelloWorldSubDirectory2, null);
            Assert.AreEqual(2, subBlobs2.Count);
        }

        [TestMethod]
        public async Task A06_UploadFile()
        {
            await using var memStream = new MemoryStream();
            await using var fileStream = File.OpenRead(_fullPathTestFile);
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            //var data = memStream.ToArray();

            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            var fullPath = BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory3 + "/" +
                           BLOB_Driver_TestConstants.TestFile1;

            var fileUploadMetadata = new FileUploadMetaData
            {
                UploadUid = Guid.NewGuid().ToString(),
                FileName = BLOB_Driver_TestConstants.TestFile1,
                RelativePath = fullPath.TrimStart('/'),
                ContentType = "image/jpeg",
                ChunkIndex = 0,
                TotalChunks = 1,
                TotalFileSize = memStream.Length
            };

            await driver.AppendBlobAsync(memStream.ToArray(), fileUploadMetadata, DateTimeOffset.UtcNow);

            var blob = await driver.GetBlobAsync(fileUploadMetadata.RelativePath);

            Assert.IsNotNull(blob);
        }

        [TestMethod]
        public async Task A07_GetAndCopyFile()
        {
            var source = BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory3 + "/" + BLOB_Driver_TestConstants.TestFile1;
            var destination = BLOB_Driver_TestConstants.HelloWorldSubDirectory2 + "/" + BLOB_Driver_TestConstants.TestFile1;

            var driver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            var sourceObject = await driver.GetBlobAsync(source);

            await driver.CopyBlobAsync(source, destination);

            var destObject = await driver.GetBlobAsync(destination);

            Assert.AreNotEqual(sourceObject.Key, destObject.Key);

            Assert.AreEqual(sourceObject.ETag, destObject.ETag);
        }
    }
}