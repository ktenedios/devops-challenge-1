using System;
using System.IO;
using Xunit;
using Kosta.DevOpsChallenge.FileProcessor;
using Kosta.DevOpsChallenge.FileProcessor.Models;
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
        public void ProcessFile_WhenFileValidatorThrowsException_ExceptionIsThrown_AndFileContentsNotCopiedToSuccessfulContainer(
            ValidationResultTypeEnum validationResult)
        {
            // Arrange
            var mockIncomingStream = new Mock<Stream>();
            var mockOutgoingStream = new Mock<Stream>();
            var mockLogger = new Mock<ILogger<Functions>>();
            var validationException = new ProductTransmissionFileValidationException(It.IsAny<string>(), validationResult);

            var mockFileValidator = new Mock<IProductTransmissionStreamReader>();
            mockFileValidator.Setup(fv => fv.ValidateStream(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()))
                             .Throws(validationException);

            // Act and Assert
            var thrownException = Assert.Throws<ProductTransmissionFileValidationException>(() =>
                Functions.ProcessFile(mockIncomingStream.Object, mockOutgoingStream.Object, "SomeFile.json", mockLogger.Object, mockFileValidator.Object)
            );

            mockOutgoingStream.Verify(outgoingStream => outgoingStream.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            Assert.Equal(validationException, thrownException);
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
            var mockFileValidator = new Mock<IProductTransmissionStreamReader>();
            mockFileValidator.Setup(fv => fv.ValidateStream(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ILogger>()))
                             .Returns(data);

            using (var processedBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(validJsonBlob, processedBlobContents, "SomeFile.json", mockLogger.Object, mockFileValidator.Object);

                // Assert
                var isMatch = serializedData.StreamMatchesStringContent(processedBlobContents);
                Assert.True(isMatch);
            }
        }
    }
}
