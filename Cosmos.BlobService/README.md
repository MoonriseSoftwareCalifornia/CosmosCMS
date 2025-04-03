# Azure Blob Storage Context

This is a an Azure Blob Storage context used by [Cosmos CMS](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS) and 
can be used by any .Net Core. project.

It is a simple wrapper around the Azure Blob Storage SDK that provides a more convenient way to interact with the Azure Blob Storage.

## Installation

How to install:

From the Nuget Package Manager Console:
```bash
Install-Package Cosmos.BlobService
```

From the .Net CLI:
```bash
dotnet add package Cosmos.BlobService
```

## Usage

How to create the context:

In the program secrets, add the connection string to the Azure Blob Storage account like this:

```json
{
  "ConnectionStrings": {
	 "AzureBlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=[YOUR-ACCOUNT-NAME];AccountKey=[YOUR-ACCOUNT-KEY]"
  }
}
```

If you are using a managed identity--and not an account key--define the connection like this:
```json
{
  "ConnectionStrings": {
	 "AzureBlobStorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=[YOUR-ACCOUNT-NAME];AccountKey=AccessToken
}
}
```

Notice the account key is set to "**AccessToken**". This is how the Azure Blob Storage context knows to use the managed identity.

### Using the context in a .Net Core project

NOTE: For a full example of this package in use, please see the Cosmos CMS project [file manager controller](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/blob/main/Editor/Controllers/FileManagerController.cs).

Meanhile, here is an example of how to use the context in a .Net Core project.

In the Startup.cs or Project.cs file, add this using statement:

```csharp
using Cosmos.BlobService;
```

And then add this line to the ConfigureServices method:
```csharp
// Add the BLOB and File Storage contexts for Cosmos CMS
builder.Services.AddCosmosStorageContext(builder.Configuration);
```

How to use the context:

In the class where you want to use the Azure Blob Storage context, inject the context like this:
```csharp
namespace Cosmos.Publisher.Controllers
{
    /// <summary>
    /// Secure file access controller and proxy.
    /// </summary>
    [AllowAnonymous]
    public class PubController : ControllerBase
    {
        private readonly StorageContext storageContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PubController"/> class.
        /// </summary>
        /// <param name="options">Cosmos options.</param>
        /// <param name="dbContext">Database context.</param>
        /// <param name="storageContext">Storage context.</param>
        public PubController(StorageContext storageContext)
        {
            this.storageContext = storageContext;
        }
    }
}
```


Here is example code for reading a file from Azure Blob Storage:
```csharp

/// <summary>
/// Gets a file and validates user authentication.
/// </summary>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public virtual async Task<IActionResult> Index(string path)
{
    var client = storageContext.GetAppendBlobClient(path);
    var properties = await client.GetPropertiesAsync();

    return File(fileStream: await client.OpenReadAsync(), contentType: properties.Value.ContentType, lastModified: properties.Value.LastModified, entityTag: new EntityTagHeaderValue(properties.Value.ETag.ToString()));
}

```

Then you can use the context to upload an image file Azure Blob Storage like this:
```csharp
/// <summary>
/// Simple image upload.
/// </summary>
public async Task<IActionResult> SimpleUpload(string directory, string entityType = "articles", string editorType = "ckeditor")
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    // Gets the file being uploaded.
    var file = Request.Form.Files.FirstOrDefault();

    if (file.Length > (1048576 * 25))
    {
        return Json(ReturnSimpleErrorMessage("The image upload failed because the image was too big (max 25MB)."));
    }

    var extension = Path.GetExtension(file.FileName).ToLower();
    var fileName = $"{Guid.NewGuid().ToString().ToLower()}{extension}";

    var image = await Image.LoadAsync(file.OpenReadStream());

    string relativePath = UrlEncode(directory + fileName);

    var contentType = MimeTypeMap.GetMimeType(Path.GetExtension(fileName));

    var metaData = new FileUploadMetaData()
    {
        ChunkIndex = 0,
        ContentType = contentType,
        FileName = fileName,
        RelativePath = relativePath,
        TotalChunks = 1,
        TotalFileSize = file.Length,
        UploadUid = Guid.NewGuid().ToString(),
        ImageHeight = image.Height.ToString(),
        ImageWidth = image.Width.ToString(),
    };

    // Read the file into a memory stream.
    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);

    // Append the blob to the storage.
    storageContext.AppendBlob(memoryStream, metaData);

    return Json(JsonConvert.DeserializeObject<dynamic>("{\"url\": \""  + "/" + relativePath + "\"}"));
    
}
```