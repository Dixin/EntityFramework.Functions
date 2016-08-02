namespace EntityFramework.Functions.Tests.Examples
{
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

        /*
        <Function Name="ParseDouble" ReturnType="Edm.Double">
            <Parameter Name="svalue" Type="Edm.String" />
             <DefiningExpression>
                cast(svalue as Edm.Double)
             </DefiningExpression>
         </Function>
         */
        [ModelDefinedFunction(nameof(ParseDouble), "EntityFramework.Functions.Tests.Examples", @"cast(svalue as Edm.Double)")]
        public static double ParseDouble(this string svalue) => double.Parse(svalue);

    }
}
