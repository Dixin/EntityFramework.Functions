namespace EntityFramework.Functions.Tests.Examples
{
    using System.Data.Entity;

    using EntityFramework.Functions.Tests.Properties;
    using EntityFramework.Functions.Tests.UnitTests;

    public partial class AdventureWorks : DbContext
    {
        static AdventureWorks()
        {
            Database.SetInitializer<AdventureWorks>(null);
            // Equivalent to: Database.SetInitializer(new NullDatabaseInitializer<AdventureWorks>());
        }

        public AdventureWorks()
            : base(Settings.Default.AdventureWorksConnectionString.ResolveDatabasePath())
        {
        }
    }
}
