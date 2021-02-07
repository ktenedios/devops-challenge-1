using System;
using System.Collections.Generic;

#nullable disable

namespace Kosta.DevOpsChallenge.FileProcessor.WarehouseDb
{
    public partial class Product
    {
        public Guid Id { get; set; }
        public string Sku { get; set; }
        public string Description { get; set; }
        public Guid CategoryId { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }
        public int Qty { get; set; }

        public virtual Category Category { get; set; }
    }
}
