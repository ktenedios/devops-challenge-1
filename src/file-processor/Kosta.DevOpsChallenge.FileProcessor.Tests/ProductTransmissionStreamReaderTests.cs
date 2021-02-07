using System;
using System.IO;
using System.Linq;
using Xunit;
using Kosta.DevOpsChallenge.FileProcessor;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests
{
    public class ProductTransmissionStreamReaderTests
    {
        [Fact]
        public void ValidateStream_EmptyStream_LogsErrorAndThrowsProductTransmissionFileValidationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            using (var processedBlobContents = new MemoryStream())
            using (var emptyBlob = new MemoryStream(0))
            {
                // Act and Assert
                var exception = Assert.Throws<ProductTransmissionFileValidationException>(
                    () => sut.ValidateStream(emptyBlob, "EmptyFile.json", mockLogger.Object));

                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'EmptyFile.json' is empty");
                Assert.Equal(ValidationResultTypeEnum.FailedEmptyFile, exception.ValidationResult);
                Assert.Equal("EmptyFile.json", exception.FileName);
            }
        }

        [Fact]
        public void ValidateStream_StreamNotJsonFormat_LogsErrorAndThrowsProductTransmissionFileValidationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            using (var processedBlobContents = new MemoryStream())
            using (var notJsonBlob = TestExtensions.GetStreamFromString("Not JSON content"))
            {
                // Act and Assert
                var exception = Assert.Throws<ProductTransmissionFileValidationException>(
                    () => sut.ValidateStream(notJsonBlob, "NotJsonBlob.json", mockLogger.Object));
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'NotJsonBlob.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedJsonDeserialization, exception.ValidationResult);
                Assert.Equal("NotJsonBlob.json", exception.FileName);
            }
        }

        [Fact]
        public void ValidateStream_SuppliedStreamIsMissingProducts_LogsErrorAndThrowsProductTransmissionFileValidationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            var data = new {
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 6,
                    qtysum = 71
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act and Assert
                var exception = Assert.Throws<ProductTransmissionFileValidationException>(
                    () => sut.ValidateStream(invalidJsonBlob, "MissingProducts.json", mockLogger.Object));

                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MissingProducts.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedMissingProducts, exception.ValidationResult);
                Assert.Equal("MissingProducts.json", exception.FileName);
            }
        }

        [Fact]
        public void ValidateStream_SuppliedStreamIsMissingTransmissionSummary_LogsErrorAndThrowsProductTransmissionFileValidationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            var product = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var data = new {
                products = new object[] { product }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act and Assert
                var exception = Assert.Throws<ProductTransmissionFileValidationException>(
                    () => sut.ValidateStream(invalidJsonBlob, "MissingTransmissionSummary.json", mockLogger.Object));

                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MissingTransmissionSummary.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedMissingTransmissionSummary, exception.ValidationResult);
                Assert.Equal("MissingTransmissionSummary.json", exception.FileName);
            }
        }

        [Fact]
        public void ValidateStream_SuppliedStreamHasMismatchedNumberOfRecords_LogsErrorAndThrowsProductTransmissionFileValidationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            var product1 = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new {
                products = new object[] { product1, product2 },
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 6,
                    qtysum = 25
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act and Assert
                var exception = Assert.Throws<ProductTransmissionFileValidationException>(
                    () => sut.ValidateStream(invalidJsonBlob, "MismatchedNumberOfRecords.json", mockLogger.Object));

                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MismatchedNumberOfRecords.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedIncorrectRecordCount, exception.ValidationResult);
                Assert.Equal("MismatchedNumberOfRecords.json", exception.FileName);
            }
        }

        [Fact]
        public void ValidateStream_SuppliedStreamHasMismatchedQuantities_LogsErrorAndThrowsProductTransmissionFileValidationException()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            var product1 = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new {
                products = new object[] { product1, product2 },
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 20
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act and Assert
                var exception = Assert.Throws<ProductTransmissionFileValidationException>(
                    () => sut.ValidateStream(invalidJsonBlob, "MismatchedQuantities.json", mockLogger.Object));

                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MismatchedQuantities.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedIncorrectQtySum, exception.ValidationResult);
                Assert.Equal("MismatchedQuantities.json", exception.FileName);
            }
        }

        [Fact]
        public void ValidateStream_SuppliedStreamIsValid_LogsInformationAndReturnsExpectedProductTransmissionObject()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new ProductTransmissionStreamReader();

            var product1 = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new {
                products = new object[] { product1, product2 },
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 25
                }
            };

            var serializedData = JsonSerializer.Serialize(data);
            ProductTransmission pt = null;

            using (var processedBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                pt = sut.ValidateStream(validJsonBlob, "ValidFile.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Information, "File 'ValidFile.json' successfully validated");

                var retrievedProduct1 = pt.products.SingleOrDefault(p =>
                    p.category == product1.category &&
                    p.description == product1.description &&
                    p.location == product1.location && 
                    p.price == product1.price &&
                    p.qty == product1.qty &&
                    p.sku == product1.sku
                );

                Assert.NotNull(retrievedProduct1);

                var retrievedProduct2 = pt.products.SingleOrDefault(p =>
                    p.category == product2.category &&
                    p.description == product2.description &&
                    p.location == product2.location && 
                    p.price == product2.price &&
                    p.qty == product2.qty &&
                    p.sku == product2.sku
                );

                Assert.NotNull(retrievedProduct2);

                Assert.Equal(data.transmissionsummary.id, pt.transmissionsummary.id);
                Assert.Equal(data.transmissionsummary.qtysum, pt.transmissionsummary.qtysum);
                Assert.Equal(data.transmissionsummary.recordcount, pt.transmissionsummary.recordcount);
            }
        }
    }
}
