using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NetCoreAzureBlobStorage.Tests
{
    public class BlobStorageServiceTests
    {
        /************* README *************/
        //To run this tests you must put your Azure Blob Storage connection strings and choose a blob file URL to test.
        /************* README *************/

        private IBlobStorageService _storageService;

        [SetUp]
        public void SetUp()
        {
            string connectionStringOne = "DefaultEndpointsProtocol=https;AccountName=sotreqlinkblob;AccountKey=6f0fR60treYCMgrnKZC7mi2RvoeWnTuJ7p/o1RVWMwfsWTtxQtUHlQytaDqzpU/CyPjSLmAyS7UQlH1s45/KMw==;EndpointSuffix=core.windows.net";
            string connectionStringTwo = "DefaultEndpointsProtocol=https;AccountName=sotreqlinkblob;AccountKey=g4GDGbXRFmK75tlkoIDenvjie4kHyztk4UamFYHkjozqpzPvtFNv+M3ydxc/1Ppg2eLM0Xn/3i7mFxEJig62ug==;EndpointSuffix=core.windows.net";

            ServiceCollection services = new ServiceCollection();

            services.AddSingleton<IBlobStorageService>(
                new BlobStorageService(connectionStringOne, connectionStringTwo));

            ServiceProvider serviceProvider = services.BuildServiceProvider();

            _storageService = serviceProvider.GetRequiredService<IBlobStorageService>();
        }

        [Test]
        public async Task DownloadFileToByteArrayTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            int fileSizeInByte = 16;

            byte[] file = await _storageService.DownloadFileToByteArray(fileUrl);

            Assert.AreEqual(file.Length, fileSizeInByte);
        }

        [Test]
        public async Task DownloadFileToDiskTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            int fileSizeInByte = 16;

            string filePath = await _storageService.DownloadFileToDisk(fileUrl);

            Assert.IsTrue(File.Exists(filePath));

            byte[] file = File.ReadAllBytes(filePath);

            Assert.AreEqual(file.Length, fileSizeInByte);
        }

        [Test]
        public async Task DownloadFileToStreamTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            int fileSizeInByte = 16;

            using Stream fileStream = await _storageService.DownloadFileToStream(fileUrl);

            DateTime downloadInitTime = DateTime.Now.AddMinutes(5);
            byte[] fileBuffer = new byte[fileSizeInByte];
            int readBytes;

            while ((readBytes = fileStream.Read(fileBuffer, 0, fileBuffer.Length)) > 0
                && DateTime.Now < downloadInitTime)
            { }

            Assert.AreEqual(fileBuffer.Length, fileSizeInByte);
        }

        [Test]
        public async Task DownloadFileToAccessSharedUrlSuccessTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            int lifeTimeInMinutes = 1;
            int fileSizeInByte = 16;

            string fileSasUrl = _storageService.DownloadFileToAccessSharedUrl(fileUrl, lifeTimeInMinutes);

            var httpClient = new HttpClient();
            HttpResponseMessage res = await httpClient.GetAsync(fileSasUrl);
            byte[] file = await res.Content.ReadAsByteArrayAsync();

            Assert.AreEqual(file.Length, fileSizeInByte);
        }

        [Test]
        public async Task DownloadFileToAccessSharedUrlExpiredTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            int urlLifeTimeInMinutes = 1;

            string fileSasUrl = _storageService.DownloadFileToAccessSharedUrl(fileUrl, urlLifeTimeInMinutes);

            Thread.Sleep(60000);

            var httpClient = new HttpClient();
            HttpResponseMessage res = await httpClient.GetAsync(fileSasUrl);

            Assert.AreEqual(res.StatusCode, HttpStatusCode.Forbidden);
        }

        [Test]
        public async Task UploadFileTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            byte[] file = await _storageService.DownloadFileToByteArray(fileUrl);

            string uniqueFileName = $"pst_mensagem_firmware/AR_FIRMWARE_{DateTime.Now:dd-MM-yyyy_HH-mm-ss}";

            string newFileUrl = await _storageService.UploadFile("oracle-lobs", uniqueFileName, file);

            Assert.IsNotNull(newFileUrl);
            Assert.IsNotEmpty(newFileUrl);
        }

        [Test]
        public async Task DeleteFileTest()
        {
            string fileUrl = "AzureBlobStorageFileURL";
            byte[] file = await _storageService.DownloadFileToByteArray(fileUrl);

            string uniqueFileName = $"pst_mensagem_firmware/AR_FIRMWARE_FOR_DELETING";

            string newFileUrl = await _storageService.UploadFile("oracle-lobs", uniqueFileName, file);

            Assert.IsNotNull(newFileUrl);
            Assert.IsNotEmpty(newFileUrl);

            await _storageService.DeleteFile(newFileUrl);
            bool newFileExistsInCloudBlobStorage = await _storageService.CheckIfFileExists(newFileUrl);

            Assert.IsFalse(newFileExistsInCloudBlobStorage);
        }

        [Test]
        public async Task CheckIfFilesExistsTest()
        {
            string fileUrlOne = "AzureBlobStorageFileURL";
            string fileUrlTwo = "AzureBlobStorageFileURL";

            var filesUrl = new List<string> { fileUrlOne, fileUrlTwo };

            Dictionary<string, bool> filesExists = await _storageService.CheckIfFilesExists(filesUrl);

            Assert.IsNotNull(filesExists);
            Assert.IsNotEmpty(filesExists);
            Assert.AreEqual(filesExists.Count, 2);
        }

        [Test]
        public async Task ListAllContainersTest()
        {
            List<string> containers = await _storageService.ListAllContainers();

            Assert.IsNotNull(containers);
        }

        [Test]
        public async Task ListAllFilesFromContainerTest()
        {
            List<string> files = await _storageService.ListAllFilesFromContainer("ContainerName", "OptionalContainerSubDirectory");

            Assert.IsNotNull(files);
        }
    }
}
