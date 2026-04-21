# File Upload API - Implementation Guide

## Overview
A complete file upload API has been implemented following the IncuSmart project architecture patterns. Files are stored in the `wwwroot/uploads` directory and tracked in the database.

## API Endpoints

### 1. Upload File
**Method:** `POST /api/file-uploads`

**Request:**
```
Form-data:
- File (IFormFile) - Required - File to upload (max 100 MB)
- Description (string) - Optional - File description (max 500 characters)
```

**Response:**
```json
{
  "statusCode": "200",
  "message": "File uploaded successfully",
  "data": {
    "id": "guid",
    "fileName": "original_filename.ext",
    "fileUrl": "/uploads/guid-hash.ext",
    "fileSize": 12345,
    "mimeType": "application/pdf"
  }
}
```

### 2. Get File by ID
**Method:** `GET /api/file-uploads/{id}`

**Response:**
```json
{
  "statusCode": "200",
  "message": "Success",
  "data": {
    "id": "guid",
    "fileName": "filename.ext",
    "fileUrl": "/uploads/filename.ext",
    "fileSize": 12345,
    "mimeType": "application/pdf"
  }
}
```

### 3. Get All Files
**Method:** `GET /api/file-uploads`

**Response:**
```json
{
  "statusCode": "200",
  "message": "Success",
  "data": [
    {
      "id": "guid",
      "fileName": "file1.pdf",
      "fileUrl": "/uploads/file1.pdf",
      "fileSize": 12345,
      "mimeType": "application/pdf"
    }
  ]
}
```

### 4. Get Files by User ID
**Method:** `GET /api/file-uploads/user/{userId}`

**Response:**
```json
{
  "statusCode": "200",
  "message": "Success",
  "data": [
    {
      "id": "guid",
      "fileName": "user_file.pdf",
      "fileUrl": "/uploads/user_file.pdf",
      "fileSize": 12345,
      "mimeType": "application/pdf"
    }
  ]
}
```

### 5. Delete File
**Method:** `DELETE /api/file-uploads/{id}`

**Response:**
```json
{
  "statusCode": "200",
  "message": "File deleted successfully",
  "data": true
}
```

## Configuration

Add to `appsettings.json`:
```json
{
  "FileUpload": {
    "BaseUrl": "http://localhost:5000/"
  }
}
```

Update `BaseUrl` based on your deployment environment.

## Allowed File Types
- Documents: `.pdf`, `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`
- Images: `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`
- Archives: `.zip`, `.rar`, `.7z`
- Data: `.txt`, `.csv`, `.json`, `.xml`

## File Constraints
- **Maximum File Size:** 100 MB
- **Storage Location:** `wwwroot/uploads`
- **File Naming:** Files are renamed with GUID to prevent conflicts

## Database Setup

Run the SQL migration to create the `file_uploads` table:
```sql
-- Execute file_uploads_migration.sql
```

Or use EF Core migrations:
```bash
dotnet ef migrations add AddFileUploads --project IncuSmart.Infra --startup-project IncuSmart.App
dotnet ef database update --project IncuSmart.Infra --startup-project IncuSmart.App
```

## Project Structure

### Core Layer
- **Domain:** `IncuSmart.Core/Domains/FileUpload.cs`
- **Commands:** `IncuSmart.Core/Commands/UploadFileCommand.cs`
- **Responses:** `IncuSmart.Core/Responses/FileUploadResponse.cs`
- **UseCase Interface:** `IncuSmart.Core/Ports/Outbound/IFileUploadUseCase.cs`
- **UseCase Implementation:** `IncuSmart.Core/Usecases/FileUploadUseCase.cs`
- **Repository Interface:** `IncuSmart.Core/Ports/Outbound/IFileUploadRepository.cs`
- **File Service Interface:** `IncuSmart.Core/Ports/Outbound/IFileUploadService.cs`

### Infrastructure Layer
- **Repository Implementation:** `IncuSmart.Infra/Persistences/Repositories/FileUploadRepository.cs`
- **Entity:** `IncuSmart.Infra/Persistences/Entities/FileUploadEntity.cs`
- **Service Implementation:** `IncuSmart.Infra/Services/FileUploadService.cs`

### API Layer
- **Controller:** `IncuSmart.API/Controllers/FileUploadController.cs`
- **Request:** `IncuSmart.API/Requests/UploadFileRequest.cs`
- **Mapper:** `IncuSmart.API/Mappers/FileUploadMapper.cs`

## Features

✅ **Soft Delete** - Files are logically deleted (DeletedAt timestamp)
✅ **File Size Validation** - Maximum 100 MB per file
✅ **File Type Validation** - Only whitelisted extensions allowed
✅ **Database Tracking** - All uploads tracked with metadata
✅ **URL Generation** - Automatic URL path generation for downloaded files
✅ **User Tracking** - Optional userId tracking for file ownership
✅ **Transaction Support** - Database operations wrapped in transactions
✅ **Error Handling** - Comprehensive error messages

## Example Usage

### cURL
```bash
# Upload file
curl -X POST http://localhost:5000/api/file-uploads \
  -F "File=@/path/to/file.pdf" \
  -F "Description=My test file"

# Get file
curl http://localhost:5000/api/file-uploads/{fileId}

# Delete file
curl -X DELETE http://localhost:5000/api/file-uploads/{fileId}
```

### C# HttpClient
```csharp
using var client = new HttpClient();
using var content = new MultipartFormDataContent();
using var fileStream = new FileStream("path/to/file.pdf", FileMode.Open);
var fileContent = new StreamContent(fileStream);
fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

content.Add(fileContent, "File", "filename.pdf");
content.Add(new StringContent("File description"), "Description");

var response = await client.PostAsync("http://localhost:5000/api/file-uploads", content);
```

## Future Enhancements

1. **Cloud Storage Integration** - Support for AWS S3, Azure Blob Storage, or Cloudinary
2. **Virus Scanning** - Integrate antivirus scanning before saving files
3. **File Compression** - Automatic compression for large files
4. **Multiple Versions** - Version control for uploaded files
5. **Access Control** - Role-based file access permissions
6. **Storage Quotas** - Per-user storage limits
7. **File Preview** - Generate thumbnails for images

## Notes

- Static files middleware has been enabled in Program.cs
- The upload directory is created automatically if it doesn't exist
- Files are stored locally in the application's `wwwroot/uploads` directory
- For production deployments, consider using cloud storage services
