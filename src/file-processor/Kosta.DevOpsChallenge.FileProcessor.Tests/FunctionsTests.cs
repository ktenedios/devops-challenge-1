using System;
using System.IO;
using Xunit;
using Kosta.DevOpsChallenge.FileProcessor;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests
{
    public class FunctionsTests
    {


        /*[Fact]
        public void ProcessFile_SuppliedFileIsValid_FileMovedToSuccessfulContainer()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

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

            using (var processedBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(validJsonBlob, processedBlobContents, "ValidFile.json", mockLogger.Object);

                // Assert
                var isMatch = serializedData.StreamMatchesStringContent(processedBlobContents);
                Assert.True(isMatch);
            }
        }*/
    }
}
