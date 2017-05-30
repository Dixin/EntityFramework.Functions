namespace EntityFramework.Functions.Tests.Examples
{
    using System;

    public static class BuiltInFunctions
    {
        [BuiltInFunction("LEFT")]
        public static string Left(this string value, int count) => Function.CallNotSupported<string>();

        [BuiltInFunction("LIKE")]
        public static string Like(this string value, string pattern) => Function.CallNotSupported<string>();

        [BuiltInFunction("SWITCHOFFSET")]
        public static DateTimeOffset? SwitchOffset(DateTimeOffset? dateTimeOffset, int offsetValue)
        {
            return Function.CallNotSupported<DateTimeOffset?>();
        }

        [BuiltInFunction("SWITCHOFFSET")]
        public static DateTimeOffset SwitchOffset(DateTimeOffset dateTimeOffset, int offsetValue)
        {
            return Function.CallNotSupported<DateTimeOffset>();
        }
    }
}
