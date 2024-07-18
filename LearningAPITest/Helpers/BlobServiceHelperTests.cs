using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LearningAPI.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
namespace LearningAPITest.Helpers;

public class BlobServiceHelperTests
{
    private readonly BlobServiceClient _mockBlobServiceClient;
    private readonly ILogger<BlobServiceHelper> _mockLogger;
    private readonly BlobServiceHelper _blobServiceHelper;

    public BlobServiceHelperTests()
    {
        _mockBlobServiceClient = Substitute.For<BlobServiceClient>();
        _mockLogger = Substitute.For<ILogger<BlobServiceHelper>>();

        _blobServiceHelper = new BlobServiceHelper(_mockBlobServiceClient, _mockLogger);
    }

    [Fact]
    public async Task UploadBlobAsync_ValidContent_UploadsToAzure()
    {
        // Arrange
        var testContainer = "testContainer";
        var testBlobName = "testBlobName";
        var testFileContent = "testFileContent";
        var blobContainerClientMock = Substitute.For<BlobContainerClient>();
        _mockBlobServiceClient.GetBlobContainerClient(testContainer).Returns(blobContainerClientMock);

        var blobClientMock = Substitute.For<BlobClient>();
        blobContainerClientMock.GetBlobClient(testBlobName).Returns(blobClientMock);

        var mockBlobContentInfo = Substitute.For<BlobContentInfo >();
        var mockResponse = Substitute.For<Response<BlobContentInfo>>();
        mockResponse.Value.Returns(mockBlobContentInfo);

        blobClientMock.UploadAsync(Arg.Any<Stream>(), true, Arg.Any<CancellationToken>()).Returns(mockResponse);

        // Act
        var result = await _blobServiceHelper.UploadBlobAsync(testContainer, testBlobName, testFileContent, default);

        // Assert
        Assert.True(result);
        _mockLogger.Received(1).LogInformation($"Uploading blob {testBlobName} to container {testContainer}");
        _mockLogger.Received(1).LogInformation($"Blob {testBlobName} uploaded to container {testContainer} with ETag: ");
    }

    [Fact]
    public async Task UploadBlobAsync_UploadThrowsException_ReturnsFalseStatus()
    {
        // Arrange
        var testContainer = "testContainer";
        var testBlobName = "testBlobName";
        var testFileContent = "testFileContent";
        var blobContainerClientMock = Substitute.For<BlobContainerClient>();
        _mockBlobServiceClient.GetBlobContainerClient(testContainer).Returns(blobContainerClientMock);

        var blobClientMock = Substitute.For<BlobClient>();
        blobContainerClientMock.GetBlobClient(testBlobName).Returns(blobClientMock);

        var mockBlobContentInfo = Substitute.For<BlobContentInfo >();
        var mockResponse = Substitute.For<Response<BlobContentInfo>>();
        mockResponse.Value.Returns(mockBlobContentInfo);

        blobClientMock.UploadAsync(Arg.Any<Stream>(), true, Arg.Any<CancellationToken>()).Throws(new Exception("Test Exception"));

        // Act
        var result = await _blobServiceHelper.UploadBlobAsync(testContainer, testBlobName, testFileContent, default);

        // Assert
        Assert.False(result);
        _mockLogger.Received(1).LogInformation($"Uploading blob {testBlobName} to container {testContainer}");

    }
}
