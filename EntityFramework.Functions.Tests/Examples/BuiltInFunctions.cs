namespace EntityFramework.Functions.Tests.Examples
{
    public static class BuiltInFunctions
    {
        [BuiltInFunction("LEFT")]
        public static string Left(this string value, int count) => Function.CallNotSupported<string>();
    }
}
