namespace EntityFramework.Functions.Tests.UnitTests
{
    using System;
    using System.IO;
    using System.Data.SqlClient;

    using EntityFramework.Functions.Tests.Properties;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public static class TestAssembly
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext testContext)
        {
            // Initializes |DataDirectory| in connection string.
            // <connectionStrings>
            //  <add name="EntityFramework.Functions.Tests.Properties.Settings.AdventureWorksConnectionString"
            //    connectionString="Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\AdventureWorks_Data.mdf;Integrated Security=True;Connect Timeout=30"
            //    providerName="System.Data.SqlClient" />
            // </connectionStrings>
            AppDomain.CurrentDomain.SetData(
                "DataDirectory",
                // .. in path does not work. so use new DirectoryInfo(path).FullName to remove .. in path.
                new DirectoryInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Data")).FullName);

            using (SqlConnection connection = new SqlConnection(Settings.Default.AdventureWorksConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("EXEC sys.sp_configure @configname = N'clr enabled', @configvalue = 1", connection))
                {
                    command.ExecuteNonQuery();
                }

                using (SqlCommand command = new SqlCommand("RECONFIGURE", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
