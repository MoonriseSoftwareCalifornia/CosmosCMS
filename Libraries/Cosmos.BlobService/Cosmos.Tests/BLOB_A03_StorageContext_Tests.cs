using Cosmos.BlobService;
using Cosmos.BlobService.Config;
using Cosmos.BlobService.Drivers;
using Cosmos.BlobService.Models;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cosmos.Tests
{
    [TestClass]
    public class BLOB_A03_StorageContext_Tests
    {
        //private static AmazonStorageConfig _amazonStorageConfig;
        private static string _fullPathTestFile;

        //private static AzureStorageConfig _azureStorageConfig;
        private static CosmosStorageConfig _cosmosConfig;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var config = new CosmosStorageConfig
            {
                StorageConfig = new StorageConfig()
            };

            config.StorageConfig = ConfigUtilities.GetCosmosConfig().Value.StorageConfig;

            _cosmosConfig = config;

            _fullPathTestFile = Path.Combine(context.DeploymentDirectory, BLOB_Driver_TestConstants.TestFile1);
        }

        [TestMethod]
        public async Task A01_ReadFiles()
        {
            _cosmosConfig.PrimaryCloud = "azure";

            var service1 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            var result1 = await service1.GetObjectsAsync("");

            _cosmosConfig.PrimaryCloud = "amazon";

            var service2 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            var result2 = await service2.GetObjectsAsync("");

            Assert.AreEqual(1, result1.Count);
            Assert.AreEqual(1, result2.Count);
        }

        [TestMethod]
        public async Task A02_UploadFileMultiCloud()
        {
            var azureDriver = new AzureStorage(_cosmosConfig.StorageConfig.AzureConfigs.FirstOrDefault());
            var awsDriver = new AmazonStorage(_cosmosConfig.StorageConfig.AmazonConfigs.FirstOrDefault(),
                ConfigUtilities.GetMemoryCache());

            await using var memStream = new MemoryStream();
            await using var fileStream = File.OpenRead(_fullPathTestFile);
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            var data = memStream.ToArray();

            var fullPath1 = BLOB_Driver_TestConstants.HelloWorldSubDirectory2 + "/" + BLOB_Driver_TestConstants.TestFile2;

            var fileUploadMetadata1 = new FileUploadMetaData
            {
                UploadUid = Guid.NewGuid().ToString(),
                FileName = BLOB_Driver_TestConstants.TestFile2,
                RelativePath = fullPath1.TrimStart('/'),
                ContentType = "image/jpeg",
                ChunkIndex = 0,
                TotalChunks = 1,
                TotalFileSize = data.Length
            };

            _cosmosConfig.PrimaryCloud = "azure";

            var service1 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            service1.AppendBlob(memStream, fileUploadMetadata1);

            _cosmosConfig.PrimaryCloud = "amazon";

            var service2 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            var fullPath2 = BLOB_Driver_TestConstants.HelloWorldSubDirectory2 + "/" + BLOB_Driver_TestConstants.TestFile3;

            var fileUploadMetadata2 = new FileUploadMetaData
            {
                UploadUid = Guid.NewGuid().ToString(),
                FileName = BLOB_Driver_TestConstants.TestFile3,
                RelativePath = fullPath2.TrimStart('/'),
                ContentType = "image/jpeg",
                ChunkIndex = 0,
                TotalChunks = 1,
                TotalFileSize = data.Length
            };

            service2.AppendBlob(memStream, fileUploadMetadata2);

            // See if the files uploaded to Azure

            var azBlobs1 = await azureDriver.GetBlobAsync(fullPath1);
            var azBlobs2 = await azureDriver.GetBlobAsync(fullPath2);

            var awsBlobs1 = await awsDriver.GetBlobAsync(fullPath1);
            var awsBlobs2 = await awsDriver.GetBlobAsync(fullPath2);

            Assert.AreEqual(azBlobs1.Name, awsBlobs1.Key);
            Assert.AreEqual(azBlobs2.Name, awsBlobs2.Key);
        }

        [TestMethod]
        public async Task A03_RenameFolder()
        {
            _cosmosConfig.PrimaryCloud = "azure";

            var service1 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            var blobsToMove =
                await service1.GetFolderContents(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory3);
            await service1.RenameAsync(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory3,
                BLOB_Driver_TestConstants.FolderRename1);
            var blobsMoved = await service1.GetFolderContents(BLOB_Driver_TestConstants.FolderRename1);
            var blobsRemaining =
                await service1.GetFolderContents(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory3);

            // The number of blobs to move, should match the number moved
            Assert.AreEqual(blobsToMove.Count, blobsMoved.Count);
            // After the move, there should be no blobs remaining in the old location
            Assert.AreEqual(0, blobsRemaining.Count);
        }

        [TestMethod]
        public async Task A04_RenameFile()
        {
            _cosmosConfig.PrimaryCloud = "azure";

            var service1 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            var fileToRename = BLOB_Driver_TestConstants.HelloWorldSubDirectory2 + "/" + BLOB_Driver_TestConstants.TestFile3;

            var fileNewName = BLOB_Driver_TestConstants.HelloWorldSubDirectory2 + "/" + BLOB_Driver_TestConstants.RenameFile2;

            Assert.IsTrue(await service1.BlobExistsAsync(fileToRename));

            await service1.RenameAsync(fileToRename, fileNewName);

            Assert.IsFalse(await service1.BlobExistsAsync(fileToRename));
            Assert.IsTrue(await service1.BlobExistsAsync(fileNewName));
        }

        [TestMethod]
        public async Task A05_NavigateSubFolders_AmazonPrimary()
        {
            _cosmosConfig.PrimaryCloud = "amazon";

            var service1 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            //
            // Arrange sub folders with files
            //
            var folder = service1.CreateFolder(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory5);

            // Create a file to upload.
            await using var memStream = new MemoryStream();
            {
                await using var fileStream = File.OpenRead(_fullPathTestFile);
                await fileStream.CopyToAsync(memStream);
            }

            for (var i = 0; i < 5; i++)
            {
                var fileName = $"file{i}.jpg";
                var path = BLOB_Driver_TestConstants.HelloWorldSubDirectory2 + "/" + fileName;

                var fileUploadMetadata1 = new FileUploadMetaData
                {
                    UploadUid = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    RelativePath = path.TrimStart('/'),
                    ContentType = "image/jpeg",
                    ChunkIndex = 0,
                    TotalChunks = 1,
                    TotalFileSize = memStream.Length
                };

                service1.AppendBlob(memStream, fileUploadMetadata1);
            }

            var folder1 = await service1.GetFolderContents(BLOB_Driver_TestConstants.HelloWorldSubDirectory2);

            foreach (var entry in folder1)
            {
                Assert.IsNotNull(entry.Name);
                Assert.IsNotNull(entry.Path);
            }

            for (var i = 0; i < 9; i++)
            {
                var fileName = $"file{i}.jpg";
                var path = BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory5 + "/" + fileName;

                var fileUploadMetadata1 = new FileUploadMetaData
                {
                    UploadUid = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    RelativePath = path.TrimStart('/'),
                    ContentType = "image/jpeg",
                    ChunkIndex = 0,
                    TotalChunks = 1,
                    TotalFileSize = memStream.Length
                };

                service1.AppendBlob(memStream, fileUploadMetadata1);
            }

            var folder2 = await service1.GetFolderContents(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory5);

            foreach (var entry in folder2)
            {
                Assert.IsNotNull(entry.Name);
                Assert.IsNotNull(entry.Path);
            }

            Assert.AreEqual(10, folder1.Count);
            Assert.AreEqual(9, folder2.Count);
        }


        [TestMethod]
        public async Task A06_NavigateSubFolders_AzurePrimary()
        {
            _cosmosConfig.PrimaryCloud = "azure";

            var service1 =
                new StorageContext(Options.Create(_cosmosConfig), ConfigUtilities.GetMemoryCache());

            //_cosmosConfig.StorageConfig.PrimaryProvider = "amazon";

            //var service2 = new BlobService.StorageContext(Options.Create(_cosmosConfig));

            var folder1 = await service1.GetFolderContents(BLOB_Driver_TestConstants.HelloWorldSubDirectory2);

            foreach (var entry in folder1)
            {
                Assert.IsNotNull(entry.Name);
                Assert.IsNotNull(entry.Path);
            }

            var folder2 = await service1.GetFolderContents(BLOB_Driver_TestConstants.HelloWorldSubdirectory2Subdirectory5);

            foreach (var entry in folder2)
            {
                Assert.IsNotNull(entry.Name);
                Assert.IsNotNull(entry.Path);
            }

            Assert.AreEqual(10, folder1.Count);
            Assert.AreEqual(9, folder2.Count);
        }
    }
}