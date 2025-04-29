# SampleSolution API

A .NET API application that provides file storage operations using Azure Blob Storage.

## Overview

The SampleSolution API is designed to handle file operations (upload, download, delete) using Azure Blob Storage as the backend storage service. It provides a simple and secure way to manage files through RESTful API endpoints.

## Features

- **File Upload**: Upload files to Azure Blob Storage with automatic GUID-based naming
- **File Download**: Download files from Azure Blob Storage by filename
- **File Deletion**: Delete files from Azure Blob Storage by filename
- **Swagger Documentation**: API documentation available through Swagger UI
- **Global Exception Handling**: Centralized error handling with appropriate HTTP status codes
- **Logging**: Comprehensive logging for all operations

## Technologies

- .NET 9.0
- ASP.NET Core Web API
- Azure Blob Storage
- Swagger/OpenAPI

## Prerequisites

- .NET 9.0 SDK or later
- Azure Storage Account
- Visual Studio 2022 or another IDE that supports .NET development

## Setup

1. Clone the repository
2. Configure Azure Blob Storage settings in `appsettings.json`:

```json
{
  "AzureBlobSettings": {
    "ConnectionString": "YOUR_AZURE_STORAGE_CONNECTION_STRING",
    "ContainerName": "YOUR_CONTAINER_NAME"
  }
}
```

3. Build the solution:

```
dotnet build
```

4. Run the application:

```
dotnet run --project SampleSolution.Api
```

5. Access the Swagger UI at `https://localhost:<port>/swagger` to test the API endpoints

## API Endpoints

### Upload File

```
POST /api/storage/upload
```

- Request: Form data with file
- Response: JSON with generated filename

### Download File

```
GET /api/storage/download/{fileName}
```

- Request: Filename in path
- Response: File content with appropriate content type

### Delete File

```
DELETE /api/storage/delete/{fileName}
```

- Request: Filename in path
- Response: 200 OK if successful, 404 Not Found if file doesn't exist

## Project Structure

- **Controllers/**: Contains the API controllers
- **Middleware/**: Contains custom middleware, including exception handling
- **Services/**: Contains service implementations
- **Settings/**: Contains configuration classes

## Error Handling

The application uses a global exception middleware that:

- Logs all exceptions
- Returns appropriate HTTP status codes based on exception type
- Provides detailed error information in development environment
- Returns sanitized error messages in production
