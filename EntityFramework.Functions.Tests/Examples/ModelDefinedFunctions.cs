namespace EntityFramework.Functions.Tests.Examples
{
    using System;

    using EntityFramework.Functions.Tests.Library.Examples;

    public static class ModelDefinedFunctions
    {
        [ModelDefinedFunction(nameof(FormatName), "EntityFramework.Functions.Tests.Examples",
            @"(CASE 
                WHEN [Person].[Title] IS NOT NULL
                THEN [Person].[Title] + N' ' 
                ELSE N'' 
            END) + [Person].[FirstName] + N' ' + [Person].[LastName]")]
        public static string FormatName(this Person person) =>
            $"{(person.Title == null ? string.Empty : person.Title + " ")}{person.FirstName} {person.LastName}";

        [ModelDefinedFunction(nameof(ParseDecimal), "EntityFramework.Functions.Tests.Examples", "cast([Person].[BusinessEntityID] as Decimal(20,8))")]
        public static decimal ParseDecimal(this Person person) => Convert.ToDecimal(person.BusinessEntityID);
    }
}