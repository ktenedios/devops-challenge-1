using System;
using System.IO;
using Xunit;
using Kosta.DevOpsChallenge.FileProcessor;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests
{
    public class FunctionsTests
    {
        [Theory]
        [InlineData(ValidationResultTypeEnum.FailedEmptyFile)]
        [InlineData(ValidationResultTypeEnum.FailedIncorrectQtySum)]
        [InlineData(ValidationResultTypeEnum.FailedIncorrectRecordCount)]
        [InlineData(ValidationResultTypeEnum.FailedJsonDeserialization)]
        [InlineData(ValidationResultTypeEnum.FailedMissingProducts)]
        [InlineData(ValidationResultTypeEnum.FailedMissingTransmissionSummary)]
        public void ProcessFile_WhenProductTransmissionStreamReaderThrowsException_FileContentsNotCopiedToSuccessfulContainer(
            ValidationResultTypeEnum validationResult)
        {
            // Arrange
            var mockIncomingStream = new Mock<Stream>();
            var mockOutgoingStream = new Mock<Stream>();
            var mockLogger = new Mock<ILogger<Functions>>();
            var validationException = new ProductTransmissionFileValidationException(It.IsAny<string>(), validationResult);

            var mockProductTransmissionStreamReader = new Mock<IProductTransmissionStreamReader>();
            mockProductTransmissionStreamReader
                .Setup(fv => fv.ValidateStream(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()))
                .Throws(validationException);

            var mockWarehouseService = new Mock<IWarehouseService>();

            var sut = new Functions(mockProductTransmissionStreamReader.Object, mockWarehouseService.Object);

            // Act
            sut.ProcessFile(mockIncomingStream.Object, mockOutgoingStream.Object, "SomeFile.json", mockLogger.Object);

            // Assert
            mockOutgoingStream.Verify(outgoingStream => outgoingStream.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData(ValidationResultTypeEnum.FailedEmptyFile)]
        [InlineData(ValidationResultTypeEnum.FailedIncorrectQtySum)]
        [InlineData(ValidationResultTypeEnum.FailedIncorrectRecordCount)]
        [InlineData(ValidationResultTypeEnum.FailedJsonDeserialization)]
        [InlineData(ValidationResultTypeEnum.FailedMissingProducts)]
        [InlineData(ValidationResultTypeEnum.FailedMissingTransmissionSummary)]
        public void ProcessFile_WhenProductTransmissionStreamReaderThrowsException_GetWarehouseReportIsCalled_AndReportIsLoggedWithError(
            ValidationResultTypeEnum validationResult)
        {
            // Arrange
            var incomingFileName = "test-file.json";
            var mockIncomingStream = new Mock<Stream>();
            var mockOutgoingStream = new Mock<Stream>();
            var mockLogger = new Mock<ILogger<Functions>>();
            var validationException = new ProductTransmissionFileValidationException(It.IsAny<string>(), validationResult);

            var mockProductTransmissionStreamReader = new Mock<IProductTransmissionStreamReader>();
            mockProductTransmissionStreamReader
                .Setup(fv => fv.ValidateStream(It.IsAny<Stream>(), incomingFileName, It.IsAny<ILogger>()))
                .Throws(validationException);

            var mockWarehouseService = new Mock<IWarehouseService>();
            mockWarehouseService.Setup(ws => ws.GetWarehouseReport(incomingFileName, validationResult))
                .Returns($"Report for file {incomingFileName}");

            var sut = new Functions(mockProductTransmissionStreamReader.Object, mockWarehouseService.Object);

            // Act
            sut.ProcessFile(mockIncomingStream.Object, mockOutgoingStream.Object, incomingFileName, mockLogger.Object);

            // Assert
            mockWarehouseService.Verify(ws => ws.GetWarehouseReport(It.IsAny<string>(), validationResult), Times.Once);
            mockLogger.VerifyLogWasCalled(LogLevel.Error, incomingFileName);
        }

        [Fact]
        public void ProcessFile_FileContainsAlreadyProcessedTransmissionSummaryId_GetWarehouseReportIsCalled_AndReportIsLoggedWithError()
        {
            // Arrange
            var incomingFileName = "test-file.json";
            var mockLogger = new Mock<ILogger<Functions>>();

            var product1 = new Product {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new Product {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new ProductTransmission {
                products = new Product[] { product1, product2 },
                transmissionsummary = new TransmissionSummary {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 25
                }
            };

            var validationResult = ValidationResultTypeEnum.FailedAlreadyProcessedTransmissionSummaryId;
            var serializedData = JsonSerializer.Serialize(data);
            var mockProductTransmissionStreamReader = new Mock<IProductTransmissionStreamReader>();
            mockProductTransmissionStreamReader
                .Setup(fv => fv.ValidateStream(It.IsAny<Stream>(), incomingFileName, It.IsAny<ILogger>()))
                .Returns(data);

            var mockWarehouseService = new Mock<IWarehouseService>();
            mockWarehouseService.Setup(ws => ws.IsTransmissionSummaryIdAlreadyProcessed(data.transmissionsummary.id))
                .Returns(true);
            mockWarehouseService.Setup(ws => ws.GetWarehouseReport(incomingFileName, validationResult))
                .Returns($"Report for file {incomingFileName}");

            var sut = new Functions(mockProductTransmissionStreamReader.Object, mockWarehouseService.Object);

            using (var processedBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                sut.ProcessFile(validJsonBlob, processedBlobContents, incomingFileName, mockLogger.Object);

                // Assert
                mockWarehouseService.Verify(ws => ws.IsTransmissionSummaryIdAlreadyProcessed(data.transmissionsummary.id), Times.Once);
                mockWarehouseService.Verify(ws => ws.GetWarehouseReport(incomingFileName, validationResult), Times.Once);
                mockLogger.VerifyLogWasCalled(LogLevel.Error, incomingFileName);
                mockWarehouseService.Verify(ws => ws.UpdateWarehouse(data), Times.Never());
                var isMatch = serializedData.StreamMatchesStringContent(processedBlobContents);
                Assert.False(isMatch);
            }
        }

        [Fact]
        public void ProcessFile_SuppliedFileIsValid_FileContentsCopiedToSuccessfulContainer()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            var product1 = new Product {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new Product {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new ProductTransmission {
                products = new Product[] { product1, product2 },
                transmissionsummary = new TransmissionSummary {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 25
                }
            };

            var serializedData = JsonSerializer.Serialize(data);
            var mockProductTransmissionStreamReader = new Mock<IProductTransmissionStreamReader>();
            mockProductTransmissionStreamReader
                .Setup(fv => fv.ValidateStream(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()))
                .Returns(data);

            var mockWarehouseService = new Mock<IWarehouseService>();

            var sut = new Functions(mockProductTransmissionStreamReader.Object, mockWarehouseService.Object);

            using (var processedBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                sut.ProcessFile(validJsonBlob, processedBlobContents, "SomeFile.json", mockLogger.Object);

                // Assert
                var isMatch = serializedData.StreamMatchesStringContent(processedBlobContents);
                Assert.True(isMatch);
            }
        }
    }
}
