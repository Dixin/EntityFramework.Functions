namespace EntityFramework.Functions
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;

    public enum FunctionType
    {
        StoredProcedure = 0,

        TableValuedFunction,

        ComposableScalarValuedFunction,

        NonComposableScalarValuedFunction,

        AggregateFunction,

        BuiltInFunction,

        NiladicFunction,

        ModelDefinedFunction,
    }

    // <Function Name="uspGetManagerEmployees" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="false" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo">
    [AttributeUsage(AttributeTargets.Method)]
    public class FunctionAttribute : DbFunctionAttribute
    {
        /// <summary>
        /// Identifies a function which is mapped to a store-defined function.
        /// </summary>
        /// <param name="type">The type of the fuction.</param>
        /// <param name="name">The name of the function.</param>
        /// <param name="namespaceName">
        /// Required for Table Valued Functions, where it should be the same as the name of the DbContext.
        /// Do not provide for other function types.
        /// </param>
        public FunctionAttribute(FunctionType type, string name, string namespaceName = Function.CodeFirstDatabaseSchema)
            : base(namespaceName, name)
        {
            this.Type = type;

            switch (type)
            {
                case FunctionType.TableValuedFunction:
                    if (namespaceName == Function.CodeFirstDatabaseSchema)
                    {
                        throw new ArgumentException("For Table Valued Functions the namespaceName parameter must be set to the name of the DbContext class.");
                    }
					break;
                case FunctionType.ModelDefinedFunction:
                    if (namespaceName == Function.CodeFirstDatabaseSchema)
                    {
                        throw new ArgumentException("For Model Defined Functions the namespaceName parameter must be set to the namespace of the DbContext class.");
                    }
                    break;
                default:
                    if (namespaceName != Function.CodeFirstDatabaseSchema)
                    {
                        throw new ArgumentException("The namespaceName parameter may only be set for Table Valued Functions.");
                    }
                    break;
            }

            switch (type)
            {
                case FunctionType.StoredProcedure:
                case FunctionType.NonComposableScalarValuedFunction:
                    this.IsComposable = false;
                    this.IsAggregate = false;
                    this.IsBuiltIn = false;
                    this.IsNiladic = false;
                    break;

                case FunctionType.TableValuedFunction:
                case FunctionType.ComposableScalarValuedFunction:
				case FunctionType.ModelDefinedFunction:
                    this.IsComposable = true;
                    this.IsAggregate = false;
                    this.IsBuiltIn = false;
                    this.IsNiladic = false;
                    break;

                case FunctionType.AggregateFunction:
                    this.IsComposable = true;
                    this.IsAggregate = true;
                    this.IsBuiltIn = false;
                    this.IsNiladic = false;
                    break;

                case FunctionType.BuiltInFunction:
                    this.IsComposable = true;
                    this.IsAggregate = false;
                    this.IsBuiltIn = true;
                    this.IsNiladic = false;
                    break;

                case FunctionType.NiladicFunction:
                    this.IsComposable = true;
                    this.IsAggregate = false;
                    this.IsBuiltIn = true;
                    this.IsNiladic = true;
                    break;
            }
        }

        public FunctionType Type { get; }

        public bool IsComposable { get; }

        public bool IsAggregate { get; }

        public bool IsBuiltIn { get; }

        public bool IsNiladic { get; }

        public string Schema { get; set; }

        public ParameterTypeSemantics ParameterTypeSemantics { get; set; } = ParameterTypeSemantics.AllowImplicitConversion;
    }

    public class StoredProcedureAttribute : FunctionAttribute
    {
        public StoredProcedureAttribute(string name) : base(FunctionType.StoredProcedure, name, Function.CodeFirstDatabaseSchema) { }
    }

    public class NonComposableScalarValuedFunctionAttribute : FunctionAttribute
    {
        public NonComposableScalarValuedFunctionAttribute(string name) : base(FunctionType.NonComposableScalarValuedFunction, name, Function.CodeFirstDatabaseSchema) { }
    }
    public class ComposableScalarValuedFunctionAttribute : FunctionAttribute
    {
        public ComposableScalarValuedFunctionAttribute(string name) : base(FunctionType.ComposableScalarValuedFunction, name, Function.CodeFirstDatabaseSchema) { }
    }

    public class TableValuedFunctionAttribute : FunctionAttribute
    {
        /// <summary>
        /// Marks a function as mapped to a Table Valued Function.
        /// </summary>
        /// <param name="name">The name of the Table Valued Function in the data store.</param>
        /// <param name="namespaceName">The name of the <see cref="DbContext"/> class.</param>
        public TableValuedFunctionAttribute(string name, string namespaceName) : base(FunctionType.TableValuedFunction, name, namespaceName) { }
    }
    public class ModelDefinedFunctionAttribute : FunctionAttribute
    {
	    /// <summary>
	    /// Marks a function as representing a Model Defined Function.
	    /// </summary>
	    /// <param name="name">The name of the method this attribute is applied to.</param>
	    /// <param name="namespaceName">The namespace of the <see cref="DbContext"/> class.</param>
	    /// <param name="entitySql">The EntitySQL implementation of the function.</param>
	    public ModelDefinedFunctionAttribute(string name, string namespaceName, string entitySql) : base(FunctionType.ModelDefinedFunction, name, namespaceName)
	    {
		    EntitySql = entitySql;
	    }

		public string EntitySql { get; }
    }

    public class AggregateFunctionAttribute : FunctionAttribute
    {
        public AggregateFunctionAttribute(string name) : base(FunctionType.AggregateFunction, name, Function.CodeFirstDatabaseSchema) { }
    }

    public class BuiltInFunctionAttribute : FunctionAttribute
    {
        public BuiltInFunctionAttribute(string name) : base(FunctionType.BuiltInFunction, name, Function.CodeFirstDatabaseSchema) { }
    }

    public class NiladicFunctionAttribute : FunctionAttribute
    {
        public NiladicFunctionAttribute(string name) : base(FunctionType.NiladicFunction, name, Function.CodeFirstDatabaseSchema) { }
    }


    // System.Data.Linq.Mapping.ParameterAttribute
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class ParameterAttribute : Attribute
    {
        public string Name { get; set; }

        public string DbType { get; set; }

        public Type ClrType { get; set; }
    }

    // System.Data.Linq.Mapping.ResultTypeAttribute
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ResultTypeAttribute : Attribute
    {
        public ResultTypeAttribute(Type type)
        {
            this.Type = type;
        }

        public Type Type { get; }
    }
}