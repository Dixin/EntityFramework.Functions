<h1>EntityFramework.Functions</h1><p>EntityFramework.Functions library implements <a href="https://en.wikipedia.org/wiki/Entity_Framework" target="_blank">Entity Framework</a> code first support for:</p>
<ul>
    <li>
        Stored procedures, with:
        <ul>
            <li>single result type</li>
            <li>multiple result types</li>
            <li>output parameter</li>
        </ul>
    </li>
    <li>Table-valued functions</li>
    <li>
        Scalar-valued functions
        <ul>
            <li>composable</li>
            <li>non-composable</li>
        </ul>
    </li>
    <li>Aggregate functions</li>
    <li>Built-in functions</li>
    <li>Niladic functions</li>
</ul>
<p>EntityFramework.Functions library works on .NET 4.0, .NET 4.5, .NET 4.6, with <a href="https://msdn.microsoft.com/en-us/data/jj574253.aspx" target="_blank">Entity Framework 6.1.0 and later</a>. Entity Framework is the only dependency of this library.</p>
<p>It can be installed through <a href="https://www.nuget.org/packages/EntityFramework.Functions" target="_blank">Nuget</a>:</p>
<blockquote>
    <p>Install-Package EntityFramework.Functions -DependencyVersion Highest</p>
</blockquote>
<p>See:</p>
<ul>
    <li>Document: <a title="https://weblogs.asp.net/Dixin/EntityFramework.Functions" href="https://weblogs.asp.net/Dixin/EntityFramework.Functions" target="_blank">https://weblogs.asp.net/Dixin/EntityFramework.Functions</a>
    <ul>
        <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Source_code">Source code</a></li>
        <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#APIs">APIs</a>
        <ul>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#[Function]">[Function]</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#[Parameter]">[Parameter]</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#[ResultType]">[ResultType]</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#FunctionConvention_and_FunctionConvention&lt;TFunctions&gt;">FunctionConvention and FunctionConvention&lt;TFunctions&gt;</a></li>
        </ul></li>
        <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Examples">Examples</a>
        <ul>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Add_functions_to_entity_model">Add functions to entity model</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Stored procedure,_with_single_result_type">Stored procedure, with single result type</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Stored_procedure,_with_output_parameter">Stored procedure, with output parameter</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Stored_procedure,_with_multiple_result_types">Stored procedure, with multiple result types</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Table-valued_function">Table-valued function</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Scalar-valued_function,_non-composable">Scalar-valued function, non-composable</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Scalar-valued_function,_composable">Scalar-valued function, composable</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Aggregate_function">Aggregate function</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Built-in_function">Built-in function</a></li>
            <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Niladic_function">Niladic function</a></li>
        </ul></li>
        <li><a href="https://weblogs.asp.net/Dixin/EntityFramework.Functions#Version_history">Version history</a></li>
    </ul></li>
    <li>Source code: <a title="https://github.com/Dixin/EntityFramework.Functions" href="https://github.com/Dixin/EntityFramework.Functions" target="_blank">https://github.com/Dixin/EntityFramework.Functions</a></li>
    <li>Nuget package: <a title="https://www.nuget.org/packages/EntityFramework.Functions" href="https://www.nuget.org/packages/EntityFramework.Functions">https://www.nuget.org/packages/EntityFramework.Functions</a></li>
</ul>
