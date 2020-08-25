using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace NetCoreAzureBlobStorage
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Downloads file from Azure Blob Storage to a byte array in memory.
        /// </summary>
        /// <param name="fileUrl">The file's Azure Blob Storage URL.</param>
        /// <returns>The file byte array in memory.</returns>
        Task<byte[]> DownloadFileToByteArray(string fileUrl);

        /// <summary>
        /// Downloads file from Azure Blob Storage into server's disk. This method uses less memory because 
        /// the file is partially copied to disk over time.
        /// </summary>
        /// <param name="fileUrl">The file's Azure Blob Storage URL.</param>
        /// <returns>The file path in server's disk.</returns>
        Task<string> DownloadFileToDisk(string fileUrl);

        /// <summary>
        /// Downloads file from Azure Blob Storage using a Stream. This method uses less memory because the 
        /// file is partially copied to the Stream over time.
        /// </summary>
        /// <param name="fileUrl">The file's Azure Blob Storage URL.</param>
        /// <returns>The file Stream.</returns>
        Task<Stream> DownloadFileToStream(string fileUrl);

        /// <summary>
        /// Creates a shared URL that can be used to access the blob file without authentication.
        /// </summary>
        /// <param name="fileUrl">The file's Azure Blob Storage URL.</param>
        /// <param name="urlLifeTimeInMinutes">The shared URL life time. After this time the URL will not work anymore.</param>
        /// <returns>The file Stream.</returns>
        string DownloadFileToAccessSharedUrl(string fileUrl, int urlLifeTimeInMinutes);

        /// <summary>
        /// Uploads file to Azure Blob Storage. If the file already exists, it will be overwritten.
        /// </summary>
        /// <param name="fileContainer">The file's container name.</param>
        /// <param name="fileName">The file's name.</param>
        /// <param name="file">The file to be uploaded.</param>
        /// <returns>The uploaded file's Azure Blob Storage URL.</returns>
        Task<string> UploadFile(string fileContainer, string fileName, byte[] file);

        /// <summary>
        /// Deletes file from Azure Blob Storage.
        /// </summary>
        /// <param name="fileUrl">The file's Azure Blob Storage URL.</param>
        Task DeleteFile(string fileUrl);

        /// <summary>
        /// Deletes a list of file from Azure Blob Storage in parallel.
        /// </summary>
        /// <param name="filesUrl">The list of files Azure Blob Storage URL.</param>
        Task DeleteFiles(IEnumerable<string> filesUrl);

        /// <summary>
        /// Checks if a file already exists in Azure Blob Storage.
        /// </summary>
        /// <param name="fileUrl">The file's Azure Blob Storage URL.</param>
        Task<bool> CheckIfFileExists(string fileUrl);

        /// <summary>
        /// Checks if a list of files already exists in Azure Blob Storage in parallel.
        /// </summary>
        /// <param name="filesUrl">The list of files Azure Blob Storage URL.</param>
        Task<Dictionary<string, bool>> CheckIfFilesExists(IEnumerable<string> filesUrl);

        /// <summary>
        /// Fetches all containers from Azure Blob Storage.
        /// </summary>
        Task<List<string>> ListAllContainers();

        /// <summary>
        /// Fetches all files from a container.
        /// </summary>
        /// <param name="container">The container whose files will be fetched.</param>
        /// <param name="containerSubDirectory">Optional: The container's sub directory whose files will be fetched.</param>
        Task<List<string>> ListAllFilesFromContainer(string container, string containerSubDirectory = null);
    }
}
