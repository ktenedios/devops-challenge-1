using System.Linq;

namespace Kosta.DevOpsChallenge.FileProcessor.Models
{
    public class ProductTransmission
    {
        public Product[] products { get; set; }
        public TransmissionSummary transmissionsummary { get; set; }

        public ValidationResultTypeEnum ValidateObject()
        {
            if (products == null || products.Length == 0)
            {
                return ValidationResultTypeEnum.FailedMissingProducts;
            }

            if (transmissionsummary == null)
            {
                return ValidationResultTypeEnum.FailedMissingTransmissionSummary;
            }

            if (!DoRecordCountsMatch())
            {
                return ValidationResultTypeEnum.FailedIncorrectRecordCount;
            }

            if (!DoQuantitiesMatch())
            {
                return ValidationResultTypeEnum.FailedIncorrectQtySum;
            }

            return ValidationResultTypeEnum.Success;
        }

        private bool DoRecordCountsMatch()
        {
            return products.Length == transmissionsummary.recordcount;
        }

        private bool DoQuantitiesMatch()
        {
            return products.Sum(p => p.qty) == transmissionsummary.qtysum;
        }
    }
}
