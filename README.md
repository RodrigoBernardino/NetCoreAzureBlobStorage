# NetCoreAzureBlobStorage
Simple project to integrate with Azure Blob Storage.

## Instalation
This project will be added to NuGet gallery soon. For now, download this solution and add its projects to your target solution.

## How to Use
The use of this project is very simple. It is necessary to inform at least one Azure Blob Storage connection string and register the ```IBlobStorageService``` as **Singleton**:

```C#
string connectionStringOne = "AzureBlobStorageConnStringOne";
string optionalConnectionStringTwo = "AzureBlobStorageConnStringTwo";

ServiceCollection services = new ServiceCollection();

services.AddSingleton<IBlobStorageService>(
    new BlobStorageService(connectionStringOne, optionalConnectionStringTwo));
```

After that it is possible to inject the ```IBlobStorageService``` and use the methods. 

### DownloadFileToByteArray
Downloads file from Azure Blob Storage to a byte array in memory. Receives the file's Azure Blob Storage URL. Returns the file byte array in memory.

### DownloadFileToDisk
Downloads file from Azure Blob Storage into server's disk. This method uses less memory because the file is partially copied to disk over time. Receives the file's Azure Blob Storage URL. Returns the file path in server's disk.

### DownloadFileToStream
Downloads file from Azure Blob Storage using a Stream. This method uses less memory because the file is partially copied to the Stream over time. Receives the file's Azure Blob Storage URL. Returns the file Stream.

### DownloadFileToAccessSharedUrl
Creates a shared URL that can be used to access the blob file without authentication. Receives the file's Azure Blob Storage URL. Returns the shared URL.

### UploadFile
Uploads file to Azure Blob Storage. If the file already exists, it will be overwritten. Receives the file's Azure Blob Storage container, the file's name and the file byte array to be uploaded. Returns the uploaded file's Azure Blob Storage URL.

### DeleteFile
Deletes file from Azure Blob Storage. Receives the file's Azure Blob Storage URL.

### DeleteFiles
Deletes a list of file from Azure Blob Storage in parallel. Receives the list of files Azure Blob Storage URL.

### CheckIfFileExists
Checks if a file already exists in Azure Blob Storage. Receives the file's Azure Blob Storage URL.

### CheckIfFilesExists
Checks if a list of files already exists in Azure Blob Storage in parallel. Receives the list of files Azure Blob Storage URL.

### ListAllContainers
Fetches all containers from Azure Blob Storage. Returns a list of all containers name. 

### ListAllFilesFromContainer
Fetches all files from a container. Receives the container whose files will be fetched and the container's sub directory whose files will be fetched (optional). Returns a list of all container's files URL. 
