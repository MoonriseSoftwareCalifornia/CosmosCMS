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
    public class FILE_A01_StorageContext_Tests
    {
        //private static AmazonStorageConfig _amazonStorageConfig;
        private static string _fullPathTestFile;

        //private static AzureStorageConfig _azureStorageConfig;
        private static CosmosStorageConfig _cosmosConfig;

        public static FileStorageContext GetContext()
        {
            var config = _cosmosConfig.StorageConfig.AzureConfigs.FirstOrDefault(f => string.IsNullOrEmpty(f.AzureFileShare) == false);
            return new FileStorageContext(config.AzureBlobStorageConnectionString, config.AzureFileShare);
        }

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
        public async Task A00_CreateFolders()
        {
            // Arrange
            var context = GetContext();
            await context.DeleteFolderAsync("");

            // Act
            await context.CreateFolder("HelloWorld");
            await context.CreateFolder("HelloWorld/SubDirectoryA");
            await context.CreateFolder("HelloWorld/SubDirectoryA/SubSub1");
            await context.CreateFolder("HelloWorld/SubDirectoryA/SubSub2");
            await context.CreateFolder("HelloChris");

            // Assert
            var test1 = await context.GetFolderContents("");
            Assert.AreEqual(2, test1.Count);
            Assert.IsTrue(test1.Any(a => a.IsDirectory && a.Name == "HelloWorld"));
            Assert.IsTrue(test1.Any(a => a.IsDirectory && a.Name == "HelloChris"));

            var test2 = await context.GetFolderContents("HelloWorld/");
            Assert.AreEqual(1, test2.Count);
            Assert.IsTrue(test2.Any(a => a.IsDirectory && a.Name == "SubDirectoryA"));
        }

        [TestMethod]
        public async Task A02_Upload_And_Get_Files()
        {

            // Arrange
            var context = GetContext();
            await using var memStream = new MemoryStream();
            await using var fileStream = File.OpenRead(_fullPathTestFile);
            await fileStream.CopyToAsync(memStream);
            memStream.Position = 0;
            var data = memStream.ToArray();

            var fullPath1 = "HelloWorld/SubDirectoryA/" + BLOB_Driver_TestConstants.TestFile1;
            var fullPath2 = "HelloWorld/SubDirectoryA/" + BLOB_Driver_TestConstants.TestFile2;

            var fileUploadMetadata1 = new FileUploadMetaData
            {
                UploadUid = Guid.NewGuid().ToString(),
                FileName = BLOB_Driver_TestConstants.TestFile1,
                RelativePath = fullPath1,
                ContentType = "image/jpeg",
                ChunkIndex = 0,
                TotalChunks = 1,
                TotalFileSize = data.Length
            };

            // Act
            await context.AppendBlob(memStream, fileUploadMetadata1);

            var fileUploadMetadata2 = new FileUploadMetaData
            {
                UploadUid = Guid.NewGuid().ToString(),
                FileName = BLOB_Driver_TestConstants.TestFile2,
                RelativePath = fullPath2,
                ContentType = "image/jpeg",
                ChunkIndex = 0,
                TotalChunks = 1,
                TotalFileSize = data.Length
            };

            await context.AppendBlob(memStream, fileUploadMetadata2);

            // Assert
            var result1 = await context.GetFileAsync(fullPath1);
            var result2 = await context.GetFileAsync(fullPath2);

            Assert.IsTrue(result1.IsDirectory == false);
            Assert.IsTrue(result2.IsDirectory == false);

            Assert.IsTrue(result1.Name == BLOB_Driver_TestConstants.TestFile1);
            Assert.IsTrue(result2.Name == BLOB_Driver_TestConstants.TestFile2);

        }

        [TestMethod]
        public async Task A03_RenameFolder_And_GetFolders()
        {
            // Arrange
            var context = GetContext();
            var originalA = "HelloWorld/SubDirectoryA/SubSub2";
            var destA = "HelloWorld/SubDirectoryA/SubSub3";
            var originalB = "HelloWorld";
            var destB = "HelloKaren";

            // Act
            await context.RenameAsync(originalA, destA);
            await context.RenameAsync(originalB, destB);

            // Assert
            var result1 = await context.GetObjectAsync("HelloKaren/SubDirectoryA/SubSub3");
            var result2 = await context.GetObjectAsync(destB);

            Assert.IsTrue(result1.IsDirectory);
            Assert.IsFalse(result1.HasDirectories);
            Assert.AreEqual("HelloKaren/SubDirectoryA/SubSub3", result1.Path);

            Assert.IsTrue(result2.IsDirectory);
            Assert.IsTrue(result2.HasDirectories);
            Assert.AreEqual(destB, result2.Path);

        }

        [TestMethod]
        public async Task A04_RenameFile()
        {
            // Arrange
            var context = GetContext();
            var originalName = "HelloKaren/SubDirectoryA/test-image.jpg";
            var destName = "HelloKaren/SubDirectoryA/test-image-b.jpg";

            // Act
            await context.RenameAsync(originalName, destName);

            // Assert
            var result = await context.GetObjectAsync(destName);
            Assert.IsFalse(result.IsDirectory);
            Assert.AreEqual("HelloKaren/SubDirectoryA/test-image-b.jpg", result.Path);
            Assert.AreEqual("test-image-b.jpg", result.Name);
        }

        [TestMethod]
        public async Task A05_ReadFile()
        {
            // Arrange
            var context = GetContext();
            var file = await context.GetFileAsync("HelloKaren/SubDirectoryA/test-image-b.jpg");
            using var stream = new MemoryStream();

            // Act
            var readStream = await context.OpenBlobReadStreamAsync("HelloKaren/SubDirectoryA/test-image-b.jpg");
            await readStream.CopyToAsync(stream);

            // Assert
            Assert.AreEqual(file.Size, stream.Length);
        }

        [TestMethod]
        public async Task A06_CopyFile()
        {
            // Arrange
            var context = GetContext();

            // Act 
            await context.CopyAsync("HelloKaren/SubDirectoryA", "HelloChris");

            // Assert
            var result1 = await context.GetFileAsync("HelloKaren/SubDirectoryA/test-image-b.jpg");
            var result2 = await context.GetFileAsync("HelloChris/test-image-b.jpg");

            Assert.AreEqual(result1.Size, result2.Size);

            // Act
            await context.CopyAsync("HelloChris/test-image-b.jpg", "HelloKaren/SubDirectoryA/SubSub1");

            // Assert
            var result3 = await context.GetFileAsync("HelloKaren/SubDirectoryA/test-image-b.jpg");
            Assert.AreEqual("HelloKaren/SubDirectoryA/test-image-b.jpg", result3.Path);

            // Act
            await context.CopyAsync("HelloKaren", "HelloChris");

            // Assert
            var result4 = await context.GetFileAsync("HelloChris/SubDirectoryA/SubSub1/test-image-b.jpg");
            Assert.AreEqual("HelloChris/SubDirectoryA/SubSub1/test-image-b.jpg", result4.Path);

        }

        [TestMethod]
        public async Task A07_Move_Items()
        {
            // Arrange
            var context = GetContext();

            // Act
            await context.MoveAsync("HelloChris/SubDirectoryA", "HelloKaren/SubDirectoryA");

            // Assert
            var result1 = await context.GetObjectAsync("HelloKaren/SubDirectoryA/test-image2.jpg");
            Assert.AreEqual("HelloKaren/SubDirectoryA/test-image2.jpg", result1.Path);
        }

        [TestMethod]
        public async Task DeleteItems()
        {
            // Arrange
            var context = GetContext();

            // Act
            await context.DeleteFileAsync("HelloKaren/SubDirectoryA/test-image2.jpg");

            // Assert
            var result1 = await context.GetObjectAsync("HelloKaren/SubDirectoryA/test-image2.jpg");
            Assert.IsNull(result1);

            // Act
            await context.DeleteFolderAsync("HelloKaren/SubDirectoryA/");
            var result2 = await context.GetObjectAsync("HelloKaren/SubDirectoryA/");
            Assert.IsNull(result2);

            var result3 = await context.GetObjectAsync("HelloKaren");
            Assert.IsNotNull(result3);
        }
    }
}