using System;

namespace EntityFramework.Functions.Tests.Examples
{
    public static class BuiltInFunctions
    {
        [BuiltInFunction("LEFT")]
        public static string Left(this string value, int count) => Function.CallNotSupported<string>();

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
