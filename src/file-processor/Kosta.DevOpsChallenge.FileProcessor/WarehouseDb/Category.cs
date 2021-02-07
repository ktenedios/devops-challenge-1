using System;
using System.Collections.Generic;

#nullable disable

namespace Kosta.DevOpsChallenge.FileProcessor.WarehouseDb
{
    public partial class Category
    {
        public Category()
        {
            InverseParentCategory = new HashSet<Category>();
            Products = new HashSet<Product>();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public Guid? ParentCategoryId { get; set; }

        public virtual Category ParentCategory { get; set; }
        public virtual ICollection<Category> InverseParentCategory { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
