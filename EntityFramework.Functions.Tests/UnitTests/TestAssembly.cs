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
            using (SqlConnection connection = new SqlConnection(Settings.Default.AdventureWorksConnectionString.ResolveDatabasePath()))
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

        internal static string ResolveDatabasePath(this string connectionString)
        {
            // Initializes |DataDirectory| in connection string.
            // <connectionStrings>
            //  <add name="EntityFramework.Functions.Tests.Properties.Settings.AdventureWorksConnectionString"
            //    connectionString="Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=..\..\..\Data\AdventureWorks_Data.mdf;Integrated Security=True;Connect Timeout=30"
            //    providerName="System.Data.SqlClient" />
            // </connectionStrings>
            SqlConnectionStringBuilder connectionBuilder = new SqlConnectionStringBuilder(Settings.Default.AdventureWorksConnectionString);
            // .. in path does not work. so use new FileInfo(path).FullName to remove .. in path.
            connectionBuilder.AttachDBFilename = new FileInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, connectionBuilder.AttachDBFilename)).FullName;
            return connectionBuilder.ConnectionString;
        }
    }
}
