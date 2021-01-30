using System.Linq;

namespace Kosta.DevOpsChallenge.FileProcessor.Models
{
    public class ProductTransmission
    {
        public Product[] products { get; set; }
        public TransmissionSummary transmissionsummary { get; set; }

        public bool IsValid()
        {
            return products?.Length > 0 &&
                (transmissionsummary != null) &&
                DoRecordCountsMatch() &&
                DoQuantitiesMatch();
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
