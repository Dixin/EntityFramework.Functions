namespace EntityFramework.Functions.Tests.Examples
{
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
	}
}