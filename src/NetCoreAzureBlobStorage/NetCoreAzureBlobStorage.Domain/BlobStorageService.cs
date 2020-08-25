using Azure.Storage;
using Azure.Storage.Sas;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace NetCoreAzureBlobStorage
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionStringOne;
        private readonly string _connectionStringTwo;
        private readonly string _accountKeyOne;
        private readonly string _accountKeyTwo;

        public BlobStorageService(string connectionStringOne, string connectionStringTwo)
        {
            _connectionStringOne = connectionStringOne;
            _connectionStringTwo = connectionStringTwo;
            _accountKeyOne = GetAccountKeyFromConnectionString(connectionStringOne);
            _accountKeyTwo = GetAccountKeyFromConnectionString(connectionStringTwo);
        }

        /// <inheritdoc/>
        public async Task<byte[]> DownloadFileToByteArray(string fileUrl)
        {
            (_, string fileContainer, string fileName) = ParseBlobStorageUrl(fileUrl);
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(fileContainer, fileName);

            await using var ms = new MemoryStream();
            await cloudBlockBlob.DownloadToStreamAsync(ms);

            return ms.ToArray();
        }

        /// <inheritdoc/>
        public async Task<string> DownloadFileToDisk(string fileUrl)
        {
            (_, string fileContainer, string fileName) = ParseBlobStorageUrl(fileUrl);
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(fileContainer, fileName);

            await using FileStream fileStream = CreateFileStream(fileContainer, fileName);
            await cloudBlockBlob.DownloadToStreamAsync(fileStream);

            return fileStream.Name;
        }

        /// <inheritdoc/>
        public async Task<Stream> DownloadFileToStream(string fileUrl)
        {
            (_, string fileContainer, string fileName) = ParseBlobStorageUrl(fileUrl);
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(fileContainer, fileName);

            return await cloudBlockBlob.OpenReadAsync();
        }

        /// <inheritdoc/>
        public string DownloadFileToAccessSharedUrl(string fileUrl, int urlLifeTimeInMinutes)
        {
            (string fileAccount, string fileContainer, string fileName) = ParseBlobStorageUrl(fileUrl);

            BlobSasBuilder blobSasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = fileContainer,
                BlobName = fileName,
                Resource = "b", //b = blob, c = container
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(urlLifeTimeInMinutes)
            };

            blobSasBuilder.SetPermissions(BlobSasPermissions.Read);
            string blobSasToken = GetBlobSasToken(blobSasBuilder, fileAccount);

            UriBuilder fullUri = new UriBuilder()
            {
                Scheme = "https",
                Host = $"{fileAccount}.blob.core.windows.net",
                Path = $"{fileContainer}/{fileName}",
                Query = blobSasToken
            };

            return fullUri.ToString();
        }

        /// <inheritdoc/>
        public async Task<string> UploadFile(string fileContainer, string fileName, byte[] file)
        {
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(fileContainer, fileName);
            await using var ms = new MemoryStream(file);
            await cloudBlockBlob.UploadFromStreamAsync(ms);

            return cloudBlockBlob.Uri.AbsoluteUri;
        }

        /// <inheritdoc/>
        public async Task DeleteFile(string fileUrl)
        {
            (_, string fileContainer, string fileName) = ParseBlobStorageUrl(fileUrl);
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(fileContainer, fileName);

            await cloudBlockBlob.DeleteIfExistsAsync();
        }

        /// <inheritdoc/>
        public async Task DeleteFiles(IEnumerable<string> filesUrl)
        {
            var tasks = new ConcurrentBag<Task>();
            Parallel.ForEach(filesUrl, (fileUrl) =>
            {
                tasks.Add(DeleteFile(fileUrl));
            });

            await Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public async Task<bool> CheckIfFileExists(string fileUrl)
        {
            var fileExistsPair = await CheckIfFilesExistsByFile(fileUrl);

            return fileExistsPair.Value;
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, bool>> CheckIfFilesExists(IEnumerable<string> filesUrl)
        {
            var tasks = new ConcurrentBag<Task<KeyValuePair<string, bool>>>();
            Parallel.ForEach(filesUrl, (fileUrl) =>
            {
                tasks.Add(CheckIfFilesExistsByFile(fileUrl));
            });

            var fileExistsPairs = await Task.WhenAll(tasks);

            var fileExistsByFile = new Dictionary<string, bool>();
            foreach (var fileExistsPair in fileExistsPairs)
                fileExistsByFile.Add(fileExistsPair.Key, fileExistsPair.Value);

            return fileExistsByFile;
        }

        /// <inheritdoc/>
        public async Task<List<string>> ListAllContainers()
        {
            var containerList = new List<string>();
            CloudBlobClient cloudBlobClient = GetCloudBlobClient();
            BlobContinuationToken token = null;
            do
            {
                ContainerResultSegment containerResultSegment = await cloudBlobClient
                    .ListContainersSegmentedAsync(token);
                token = containerResultSegment.ContinuationToken;
                foreach (CloudBlobContainer cloudBlobContainer in containerResultSegment.Results)
                    containerList.Add(cloudBlobContainer.Name);
            } while (token != null);

            return containerList;
        }

        /// <inheritdoc/>
        public async Task<List<string>> ListAllFilesFromContainer(string container,
            string containerSubDirectory = null)
        {
            var fileList = new List<string>();
            if (string.IsNullOrWhiteSpace(containerSubDirectory))
                containerSubDirectory = string.Empty;

            CloudBlobClient cloudBlobClient = GetCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(container);

            BlobContinuationToken token;
            do
            {
                BlobResultSegment blobResultSegment = await cloudBlobContainer
                    .ListBlobsSegmentedAsync(containerSubDirectory, true, BlobListingDetails.None,
                        int.MaxValue, null, null, null);
                token = blobResultSegment.ContinuationToken;
                foreach (IListBlobItem listBlobItem in blobResultSegment.Results)
                    if (!listBlobItem.Uri.AbsoluteUri.EndsWith(containerSubDirectory))
                        fileList.Add(listBlobItem.Uri.AbsoluteUri);
            } while (token != null);

            return fileList;
        }

        private async Task<KeyValuePair<string, bool>> CheckIfFilesExistsByFile(string fileUrl)
        {
            (_, string fileContainer, string fileName) = ParseBlobStorageUrl(fileUrl);
            CloudBlockBlob cloudBlockBlob = GetCloudBlockBlob(fileContainer, fileName);

            bool fileExists = await cloudBlockBlob.ExistsAsync();

            return new KeyValuePair<string, bool>(fileUrl, fileExists);
        }

        private CloudBlockBlob GetCloudBlockBlob(string fileContainer, string fileName)
        {
            CloudBlobClient cloudBlobClient = GetCloudBlobClient();
            CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference(fileContainer);
            CloudBlockBlob cloudBlob = cloudBlobContainer.GetBlockBlobReference(fileName);

            return cloudBlob;
        }

        private CloudBlobClient GetCloudBlobClient()
        {
            CloudStorageAccount cloudStorageAccount;

            try
            {
                cloudStorageAccount = CloudStorageAccount.Parse(_connectionStringOne);
            }
            catch
            {
                cloudStorageAccount = CloudStorageAccount.Parse(_connectionStringTwo);
            }

            return cloudStorageAccount.CreateCloudBlobClient();
        }

        private static (string fileAccount, string fileContainer, string fileName)
            ParseBlobStorageUrl(string fileUrl)
        {
            string fileAccount = null, fileContainer = null, fileName = null;
            string[] fileUrlParts = fileUrl.Split('/');

            if (fileUrlParts.Length >= 3)
            {
                string[] fileAccountNameParts = fileUrlParts[2].Split('.');
                fileAccount = fileAccountNameParts[0];
            }

            if (fileUrlParts.Length >= 4)
            {
                fileContainer = fileUrlParts[3];
            }

            if (fileUrlParts.Length >= 6)
            {
                fileName = $"{fileUrlParts[4]}/{fileUrlParts[5]}";
            }

            if (string.IsNullOrWhiteSpace(fileAccount)
                    || string.IsNullOrWhiteSpace(fileContainer)
                    || string.IsNullOrWhiteSpace(fileName))
                throw new ApplicationException("Error to parse blob file URL. Blob container or Blob name not found.");

            return (fileAccount, fileContainer, fileName);
        }

        private static FileStream CreateFileStream(string fileContainer, string fileName)
        {
            string rootDirectoryPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string blobStorageFilesDirectoryPath = $"{rootDirectoryPath}/BlobStorageFiles";
            Directory.CreateDirectory(blobStorageFilesDirectoryPath);

            string fileFullName = $"{fileContainer}_{fileName.Replace('/', '-')}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

            return new FileStream($"{blobStorageFilesDirectoryPath}/{fileFullName}", FileMode.Create);
        }

        private static string GetAccountKeyFromConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                return null; 

            string[] connStringParts = connectionString.Split(';');
            string accountKeyParam = connStringParts[2];
            int firstEqualIndex = accountKeyParam.IndexOf('=');

            return accountKeyParam.Substring(firstEqualIndex + 1);
        }

        private string GetBlobSasToken(BlobSasBuilder blobSasBuilder, string fileAccount)
        {
            try
            {
                var storageSharedKeyCredential = new StorageSharedKeyCredential(fileAccount, _accountKeyOne);
                return blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            }
            catch
            {
                var storageSharedKeyCredential = new StorageSharedKeyCredential(fileAccount, _accountKeyTwo);
                return blobSasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            }
        }
    }
}
