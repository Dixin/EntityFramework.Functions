namespace EntityFramework.Functions.Tests.Examples
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity;

    using EntityFramework.Functions.Tests.Library.Examples;
    
    public partial class AdventureWorks
    {
        public const string Production = nameof(Production);

        public DbSet<ProductCategory> ProductCategories { get; set; }

        public DbSet<ProductSubcategory> ProductSubcategories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<Person> Persons { get; set; }
    }

    [Table(nameof(ProductCategory), Schema = AdventureWorks.Production)]
    public class ProductCategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductCategoryID { get; set; }

        [MaxLength(50)]
        public string Name { get; set; }

        public virtual ICollection<ProductSubcategory> ProductSubcategories { get; } = new HashSet<ProductSubcategory>();
    }

    [Table(nameof(ProductSubcategory), Schema = AdventureWorks.Production)]
    public class ProductSubcategory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductSubcategoryID { get; set; }

        [MaxLength(50)]
        public string Name { get; set; }

        public int? ProductCategoryID { get; set; }

        public ProductCategory ProductCategory { get; set; }

        public ICollection<Product> Products { get; set; } = new HashSet<Product>();
    }

    [Table(nameof(Product), Schema = AdventureWorks.Production)]
    public class Product
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }

        [MaxLength(50)]
        public string Name { get; set; }

        public decimal ListPrice { get; set; }

        public int? ProductSubcategoryID { get; set; }

        public ProductSubcategory ProductSubcategory { get; set; }
    }
}
