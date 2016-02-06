namespace EntityFramework.Functions.Tests.Examples
{
	public static class ModelDefinedFunctions
	{
		[ModelDefinedFunction(nameof(FormatName), "EntityFramework.Functions.Tests.Examples",
			"CASE WHEN person.Title IS NOT NULL THEN person.Title + ' ' ELSE '' END " +
			"+ person.FirstName + ' ' + person.LastName")]
		public static string FormatName(this Person person) => string.Format("{0}{1} {2}",
																			 person.Title == null ? "" : person.Title + " ",
																			 person.FirstName,
																			 person.LastName);
	}
}