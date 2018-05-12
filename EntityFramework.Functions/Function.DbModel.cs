namespace EntityFramework.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Mapping;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using System.Reflection;

    public static partial class Function
    {
        public static void AddFunctions(this DbModel model, Type functionsType)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (functionsType == null)
            {
                throw new ArgumentNullException(nameof(functionsType));
            }

            functionsType
                .GetMethods(BindingFlags.Public | BindingFlags.InvokeMethod
                    | BindingFlags.Instance | BindingFlags.Static)
                .Select(methodInfo => new
                {
                    MethodInfo = methodInfo,
                    FunctionAttribute = methodInfo.GetCustomAttribute<FunctionAttribute>()
                })
                .Where(method => method.FunctionAttribute != null)
                .ForEach(method => model.AddFunction(method.MethodInfo, method.FunctionAttribute));
        }

        public static void AddFunction(
            this DbModel model,
            MethodInfo methodInfo,
            FunctionAttribute functionAttribute)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (methodInfo == null)
            {
                throw new ArgumentNullException(nameof(methodInfo));
            }

            if (functionAttribute == null)
            {
                throw new ArgumentNullException(nameof(functionAttribute));
            }

            if (functionAttribute.Type == FunctionType.ModelDefinedFunction)
            {
                AddModelDefinedFunction(model, methodInfo, (ModelDefinedFunctionAttribute)functionAttribute);
                return;
            }

            /*
    <!-- SSDL content -->
    <edmx:StorageModels>
      <Schema Namespace="CodeFirstDatabaseSchema" Provider="System.Data.SqlClient" ProviderManifestToken="2012" Alias="Self" xmlns:store="http://schemas.microsoft.com/ado/2007/12/edm/EntityStoreSchemaGenerator" xmlns:customannotation="http://schemas.microsoft.com/ado/2013/11/edm/customannotation" xmlns="http://schemas.microsoft.com/ado/2009/11/edm/ssdl">
        <Function Name="ufnGetContactInformation" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="true" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo">
          <Parameter Name="PersonID" Type="int" Mode="In" />
          <ReturnType>
            <CollectionType>
              <RowType>
                <Property Name="PersonID" Type="int" Nullable="false" />
                <Property Name="FirstName" Type="nvarchar" MaxLength="50" />
                <Property Name="LastName" Type="nvarchar" MaxLength="50" />
                <Property Name="JobTitle" Type="nvarchar" MaxLength="50" />
                <Property Name="BusinessEntityType" Type="nvarchar" MaxLength="50" />
              </RowType>
            </CollectionType>
          </ReturnType>
        </Function>
        <Function Name="ufnGetProductListPrice" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="true" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo" ReturnType="money">
          <Parameter Name="ProductID" Type="int" Mode="In" />
          <Parameter Name="OrderDate" Type="datetime" Mode="In" />
        </Function>
        <Function Name="ufnGetProductStandardCost" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="false" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo">
          <Parameter Name="ProductID" Type="int" Mode="In" />
          <Parameter Name="OrderDate" Type="datetime" Mode="In" />
          <CommandText>
            SELECT [dbo].[ufnGetProductListPrice](@ProductID, @OrderDate)
          </CommandText>
        </Function>
        <Function Name="uspGetCategoryAndSubCategory" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="false" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo">
          <Parameter Name="CategoryID" Type="int" Mode="In" />
        </Function>
        <Function Name="uspGetManagerEmployees" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="false" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo">
          <Parameter Name="BusinessEntityID" Type="int" Mode="In" />
        </Function>
        <EntityContainer Name="CodeFirstDatabase">
        </EntityContainer>
        <Function Name="ufnGetPersons" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="true" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo">
          <Parameter Name="Name" Type="nvarchar" Mode="In" />
          <ReturnType>
            <CollectionType>
              <RowType>
                <Property Name="BusinessEntityID" Type="int" Nullable="false" />
                <Property Name="Title" Type="nvarchar" MaxLength="8" />
                <Property Name="FirstName" Type="nvarchar" MaxLength="50" Nullable="false" />
                <Property Name="LastName" Type="nvarchar" MaxLength="50" Nullable="false" />
              </RowType>
            </CollectionType>
          </ReturnType>
        </Function>
      </Schema>
    </edmx:StorageModels>
            */
            // Build above <StorageModels> imperatively.
            string functionName = functionAttribute.FunctionName;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                functionName = methodInfo.Name;
            }

            //Fix (rodro75): functions could be added several times here in case of methods overloading, 
            //which is necessary in some scenarios, but we should really add the metadata just once or EF
            //would complain when compiling the model.
            //Not shure about "Ordinal" equality though.. shouldn't it be OrdinalIgnoreCase instead?
            //As far as I know function names are not case-sensitive in SQL Server.
            if (model.StoreModel.Functions.Any(x => x.Name.EqualsOrdinal(functionName)))
                return;

            EdmFunction storeFunction = EdmFunction.Create(
                functionName,
                FunctionAttribute.CodeFirstDatabaseSchema, // model.StoreModel.Container.Name is always "CodeFirstDatabaseSchema".
                DataSpace.SSpace, // <edmx:StorageModels>
                new EdmFunctionPayload()
                {
                    Schema = functionAttribute.Schema,
                    StoreFunctionName = functionAttribute.StoreFunctionName,
                    IsAggregate = functionAttribute.IsAggregate,
                    IsBuiltIn = functionAttribute.IsBuiltIn,
                    IsNiladic = functionAttribute.IsNiladic,
                    IsComposable = functionAttribute.IsComposable,
                    ParameterTypeSemantics = functionAttribute.ParameterTypeSemantics,
                    Parameters = model.GetStoreParameters(methodInfo, functionAttribute),
                    ReturnParameters = model.GetStoreReturnParameters(methodInfo, functionAttribute),
                    CommandText = methodInfo.GetStoreCommandText(functionAttribute, functionName),

                },
                null);
            model.StoreModel.AddItem(storeFunction);

            switch (functionAttribute.Type)
            {
                // Aggregate/Built in/Niladic/Composable scalar-valued function has no <FunctionImport> or <FunctionImportMapping>.
                case FunctionType.ComposableScalarValuedFunction:
                case FunctionType.AggregateFunction:
                case FunctionType.BuiltInFunction:
                case FunctionType.NiladicFunction:
                case FunctionType.ModelDefinedFunction:
                    return;
            }

            /*
    <!-- CSDL content -->
    <edmx:ConceptualModels>
      <Schema Namespace="AdventureWorks" Alias="Self" annotation:UseStrongSpatialTypes="false" xmlns:annotation="http://schemas.microsoft.com/ado/2009/02/edm/annotation" xmlns:customannotation="http://schemas.microsoft.com/ado/2013/11/edm/customannotation" xmlns="http://schemas.microsoft.com/ado/2009/11/edm">
        <EntityContainer Name="AdventureWorks" annotation:LazyLoadingEnabled="true">
          <FunctionImport Name="ufnGetContactInformation" IsComposable="true" ReturnType="Collection(AdventureWorks.ContactInformation)">
            <Parameter Name="PersonID" Mode="In" Type="Int32" />
          </FunctionImport>
          <FunctionImport Name="uspGetCategoryAndSubCategory" ReturnType="Collection(AdventureWorks.CategoryAndSubCategory)">
            <Parameter Name="CategoryID" Mode="In" Type="Int32" />
          </FunctionImport>
          <FunctionImport Name="uspGetManagerEmployees" ReturnType="Collection(AdventureWorks.ManagerEmployee)">
            <Parameter Name="BusinessEntityID" Mode="In" Type="Int32" />
          </FunctionImport>
          <FunctionImport Name="ufnGetProductStandardCost" ReturnType="Collection(Decimal)">
            <Parameter Name="ProductID" Mode="In" Type="Int32" />
            <Parameter Name="OrderDate" Mode="In" Type="DateTime" />
          </FunctionImport>
          <FunctionImport Name="ufnGetPersons" IsComposable="true" EntitySet="Persons" ReturnType="Collection(Model.Person)">
            <Parameter Name="Name" Mode="In" Type="String" />
          </FunctionImport>
        </EntityContainer>
      </Schema>
    </edmx:ConceptualModels>
            */
            // Build above <ConceptualModels> imperatively.
            EdmFunction modelFunction = EdmFunction.Create(
                storeFunction.Name,
                model.ConceptualModel.Container.Name,
                DataSpace.CSpace, // <edmx:ConceptualModels>
                new EdmFunctionPayload
                {
                    IsFunctionImport = true,
                    IsComposable = storeFunction.IsComposableAttribute,
                    Parameters = model.GetModelParameters(methodInfo, storeFunction),
                    ReturnParameters = model.GetModelReturnParameters(methodInfo, functionAttribute),
                    EntitySets = model.GetModelEntitySets(methodInfo, functionAttribute)
                },
                null);
            model.ConceptualModel.Container.AddFunctionImport(modelFunction);

            /*
    <!-- C-S mapping content -->
    <edmx:Mappings>
      <Mapping Space="C-S" xmlns="http://schemas.microsoft.com/ado/2009/11/mapping/cs">
        <EntityContainerMapping StorageEntityContainer="CodeFirstDatabase" CdmEntityContainer="AdventureWorks">
          <FunctionImportMapping FunctionImportName="ufnGetContactInformation" FunctionName="AdventureWorks.ufnGetContactInformation">
            <ResultMapping>
              <ComplexTypeMapping TypeName="AdventureWorks.ContactInformation">
                <ScalarProperty Name="PersonID" ColumnName="PersonID" />
                <ScalarProperty Name="FirstName" ColumnName="FirstName" />
                <ScalarProperty Name="LastName" ColumnName="LastName" />
                <ScalarProperty Name="JobTitle" ColumnName="JobTitle" />
                <ScalarProperty Name="BusinessEntityType" ColumnName="BusinessEntityType" />
              </ComplexTypeMapping>
            </ResultMapping>
          </FunctionImportMapping>
          <FunctionImportMapping FunctionImportName="uspGetCategoryAndSubCategory" FunctionName="AdventureWorks.uspGetCategoryAndSubCategory">
            <ResultMapping>
              <ComplexTypeMapping TypeName="AdventureWorks.CategoryAndSubCategory">
                <ScalarProperty Name="ProductCategoryID" ColumnName="ProductCategoryID" />
                <ScalarProperty Name="Name" ColumnName="Name" />
              </ComplexTypeMapping>
            </ResultMapping>
          </FunctionImportMapping>
          <FunctionImportMapping FunctionImportName="uspGetManagerEmployees" FunctionName="AdventureWorks.uspGetManagerEmployees">
            <ResultMapping>
              <ComplexTypeMapping TypeName="AdventureWorks.ManagerEmployee">
                <ScalarProperty Name="RecursionLevel" ColumnName="RecursionLevel" />
                <ScalarProperty Name="OrganizationNode" ColumnName="OrganizationNode" />
                <ScalarProperty Name="ManagerFirstName" ColumnName="ManagerFirstName" />
                <ScalarProperty Name="ManagerLastName" ColumnName="ManagerLastName" />
                <ScalarProperty Name="BusinessEntityID" ColumnName="BusinessEntityID" />
                <ScalarProperty Name="FirstName" ColumnName="FirstName" />
                <ScalarProperty Name="LastName" ColumnName="LastName" />
              </ComplexTypeMapping>
            </ResultMapping>
          </FunctionImportMapping>
          <FunctionImportMapping FunctionImportName="ufnGetProductStandardCost" FunctionName="AdventureWorks.ufnGetProductStandardCost" />
          <FunctionImportMapping FunctionImportName="ufnGetPersons" FunctionName="Model.Store.ufnGetPersons" />
        </EntityContainerMapping>
      </Mapping>
    </edmx:Mappings>
            */
            // Build above <Mappings> imperatively.
            if (modelFunction.IsComposableAttribute)
            {
                model.ConceptualToStoreMapping.AddFunctionImportMapping(new FunctionImportMappingComposable(
                    modelFunction,
                    storeFunction,
                    new FunctionImportResultMapping(),
                    model.ConceptualToStoreMapping));
            }
            else
            {
                model.ConceptualToStoreMapping.AddFunctionImportMapping(new FunctionImportMappingNonComposable(
                    modelFunction,
                    storeFunction,
                    Enumerable.Empty<FunctionImportResultMapping>(),
                    model.ConceptualToStoreMapping));
            }
        }

        private static void AddModelDefinedFunction(DbModel model, MethodInfo methodInfo, ModelDefinedFunctionAttribute functionAttribute)
        {
            /*
            <!-- CSDL content -->
            <edmx:ConceptualModels>
                <Schema Namespace="AdventureWorks" Alias="Self" annotation:UseStrongSpatialTypes="false" xmlns:annotation="http://schemas.microsoft.com/ado/2009/02/edm/annotation" xmlns:customannotation="http://schemas.microsoft.com/ado/2013/11/edm/customannotation" xmlns="http://schemas.microsoft.com/ado/2009/11/edm">
                    <Function Name="FormatName" ReturnType="Edm.String>
                        <Parameter Name="person" Type="AdventureWorks.Person" />
                        <DefiningExpression>
                            CASE WHEN person.Title IS NOT NULL THEN person.Title + ' ' ELSE '' END + person.FirstName + ' ' + person.LastName
                        </DefiningExpression>
                    </Function>
                </Schema>
            </edmx:ConceptualModels>
            */
            // Build above <ConceptualModels> imperatively.

            string modelNamespaceName = model.ConceptualModel.EntityTypes.Select(e => e.NamespaceName).FirstOrDefault();
            if (functionAttribute.NamespaceName != modelNamespaceName)
            {
                throw new InvalidOperationException($"The ModelDefinedFunctionAttribute for method {methodInfo.Name} must have namespaceName set to '{modelNamespaceName}'.");
            }

            EdmFunction modelFunction = EdmFunction.Create(
                methodInfo.Name,
                modelNamespaceName,
                DataSpace.CSpace, // <edmx:ConceptualModels>
                new EdmFunctionPayload
                {
                    IsComposable = true,
                    Parameters = model.GetModelParametersForModelDefinedFunction(methodInfo),
                    ReturnParameters = model.GetModelReturnParameters(methodInfo, functionAttribute),
                    EntitySets = model.GetModelEntitySets(methodInfo, functionAttribute),
                    CommandText = functionAttribute.EntitySql,
                },
                null);
            model.ConceptualModel.AddItem(modelFunction);
        }

        private static IList<FunctionParameter> GetStoreParameters
            (this DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute) => methodInfo
                .GetParameters()
                .Select((parameterInfo, index) =>
                    {
                        string parameterName = parameterInfo.GetCustomAttribute<ParameterAttribute>()?.Name;
                        if (string.IsNullOrWhiteSpace(parameterName))
                        {
                            parameterName = parameterInfo.Name;
                        }

                        switch (functionAttribute.Type)
                        {
                            case FunctionType.NiladicFunction:
                                throw new NotSupportedException(
                                    $"Parameter of method {methodInfo.Name} is not supported.");

                            case FunctionType.AggregateFunction:
                                {
                                    if (index == 0)
                                    {
                                        return FunctionParameter.Create(
                                            parameterName,
                                            model.GetStoreParameterPrimitiveType(
                                                    methodInfo, parameterInfo, functionAttribute)
                                                .GetCollectionType(), // Must be collection type.
                                            ParameterMode.In);
                                    }

                                    // Aggregate function with more than more parameter is not supported by entity framework.
                                    throw new NotSupportedException(
                                        $"Method {methodInfo.Name} has more than one parameters and is not supported by Entity Framework.");
                                }
                        }

                        return FunctionParameter.Create(
                            parameterName,
                            model.GetStoreParameterPrimitiveType(methodInfo, parameterInfo, functionAttribute),
                            parameterInfo.ParameterType == typeof(ObjectParameter)
                                ? ParameterMode.InOut
                                : ParameterMode.In);
                    })
                .ToArray();

        private static PrimitiveType GetStoreParameterPrimitiveType(
            this DbModel model, MethodInfo methodInfo, ParameterInfo parameterInfo, FunctionAttribute functionAttribute)
        {
            // <Parameter Name="PersonID" Type="int" Mode="In" />
            Type parameterClrType = parameterInfo.ParameterType;
            ParameterAttribute parameterAttribute = parameterInfo.GetCustomAttribute<ParameterAttribute>();
            Type parameterAttributeClrType = parameterAttribute?.ClrType;

            if (parameterClrType.IsGenericType)
            {
                Type parameterClrTypeDefinition = parameterClrType.GetGenericTypeDefinition();
                if (parameterClrTypeDefinition == typeof(IEnumerable<>)
                    || parameterClrTypeDefinition == typeof(IQueryable<>))
                {
                    if (functionAttribute.Type == FunctionType.AggregateFunction)
                    {
                        // Aggregate function has one IEnumerable<T> or IQueryable<T> parameter. 
                        parameterClrType = parameterClrType.GetGenericArguments().Single();
                    }
                    else
                    {
                        throw new NotSupportedException(
                            $"Parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported. {typeof(IEnumerable<>).FullName} parameter must be used for {nameof(FunctionType)}.{nameof(FunctionType.AggregateFunction)} method.");
                    }
                }
            }

            if (parameterClrType == typeof(ObjectParameter))
            {
                // ObjectParameter must be used for stored procedure parameter.
                if (functionAttribute.Type != FunctionType.StoredProcedure)
                {
                    throw new NotSupportedException(
                        $"Parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported. {nameof(ObjectParameter)} parameter must be used for {nameof(FunctionType)}.{nameof(FunctionType.StoredProcedure)} method.");
                }

                // ObjectParameter.Type is available only when methodInfo is called. 
                // When building model, its store type/clr type must be provided by ParameterAttribute.
                if (parameterAttributeClrType == null)
                {
                    throw new NotSupportedException(
                        $"Parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported. {nameof(ObjectParameter)} parameter must have {nameof(ParameterAttribute)} with {nameof(ParameterAttribute.ClrType)} specified, with optional {nameof(ParameterAttribute.DbType)}.");
                }

                parameterClrType = parameterAttributeClrType;
            }
            else
            {
                // When parameter is not ObjectParameter, ParameterAttribute.ClrType should be either not specified, or the same as parameterClrType.
                if (parameterAttributeClrType != null && parameterAttributeClrType != parameterClrType)
                {
                    throw new NotSupportedException(
                        $"Parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported. It is of {parameterClrType.FullName} type, but its {nameof(ParameterAttribute)}.{nameof(ParameterAttribute.ClrType)} has a different type {parameterAttributeClrType.FullName}");
                }
            }

            string storePrimitiveTypeName = parameterAttribute?.DbType;
            return !string.IsNullOrEmpty(storePrimitiveTypeName)
                ? model.GetStorePrimitiveType(storePrimitiveTypeName, methodInfo, parameterInfo)
                : model.GetStorePrimitiveType(parameterClrType, methodInfo, parameterInfo);
        }

        private static PrimitiveType GetStorePrimitiveType(
            this DbModel model, string storeEdmTypeName, MethodInfo methodInfo, ParameterInfo parameterInfo)
        {
            // targetStoreEdmType = model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(primitiveEdmType)).EdmType;
            PrimitiveType storePrimitiveType = model
                .ProviderManifest
                .GetStoreTypes()
                .FirstOrDefault(primitiveType => primitiveType.Name.EqualsOrdinal(storeEdmTypeName));
            if (storePrimitiveType == null)
            {
                throw new NotSupportedException(
                    $"The specified {nameof(ParameterAttribute)}.{nameof(ParameterAttribute.DbType)} '{storeEdmTypeName}' for parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported in database.");
            }

            return storePrimitiveType;
        }

        private static PrimitiveType GetStorePrimitiveType(
            this DbModel model, Type clrType, MethodInfo methodInfo, ParameterInfo parameterInfo)
        {
            // targetStoreEdmType = model.ProviderManifest.GetStoreType(TypeUsage.CreateDefaultTypeUsage(primitiveEdmType)).EdmType;
            PrimitiveType storePrimitiveType = model
                .ProviderManifest
                .GetStoreTypes()
                .FirstOrDefault(primitiveType => primitiveType.ClrEquivalentType == (Nullable.GetUnderlyingType(clrType) ?? clrType));
            if (storePrimitiveType == null)
            {
                throw new NotSupportedException(
                    $"The specified type {clrType.FullName} for parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported in database.");
            }

            return storePrimitiveType;
        }

        private static IList<FunctionParameter> GetStoreReturnParameters(
            this DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
        {
            ParameterInfo returnParameterInfo = methodInfo.ReturnParameter;
            if (returnParameterInfo == null || returnParameterInfo.ParameterType == typeof(void))
            {
                throw new NotSupportedException($"The return type of {methodInfo.Name} is not supported.");
            }

            ParameterAttribute returnParameterAttribute = returnParameterInfo.GetCustomAttribute<ParameterAttribute>();
            ResultTypeAttribute[] returnTypeAttributes = methodInfo.GetCustomAttributes<ResultTypeAttribute>().ToArray();

            if (functionAttribute.Type == FunctionType.StoredProcedure)
            {
                if (returnParameterAttribute != null)
                {
                    throw new NotSupportedException(
                        $"{nameof(ParameterAttribute)} for return value of method {methodInfo.Name} is not supported.");
                }

                return new FunctionParameter[0];
            }

            if (returnTypeAttributes.Any())
            {
                throw new NotSupportedException($"{nameof(ResultTypeAttribute)} for method {methodInfo.Name} is not supported.");
            }

            if (functionAttribute.Type == FunctionType.TableValuedFunction)
            {
                if (returnParameterAttribute != null)
                {
                    throw new NotSupportedException(
                        $"{nameof(ParameterAttribute)} for return value of method {methodInfo.Name} is not supported.");
                }

                /*
        <CollectionType>
          <RowType>
            <Property Name="PersonID" Type="int" Nullable="false" />
            <Property Name="FirstName" Type="nvarchar" MaxLength="50" />
            <Property Name="LastName" Type="nvarchar" MaxLength="50" />
            <Property Name="JobTitle" Type="nvarchar" MaxLength="50" />
            <Property Name="BusinessEntityType" Type="nvarchar" MaxLength="50" />
          </RowType>
        </CollectionType>
                */
                // returnParameterInfo.ParameterType is IQueryable<T>.
                Type storeReturnParameterClrType = returnParameterInfo.ParameterType.GetGenericArguments().Single();
                StructuralType modelReturnParameterStructuralType = model.GetModelStructualType(
                    storeReturnParameterClrType, methodInfo);
                ComplexType modelReturnParameterComplexType = modelReturnParameterStructuralType as ComplexType;
                RowType storeReturnParameterRowType;
                if (modelReturnParameterComplexType != null)
                {
                    storeReturnParameterRowType = RowType.Create(
                        modelReturnParameterComplexType.Properties.Select(property =>
                            EdmProperty.Create(property.Name, model.ProviderManifest.GetStoreType(property.TypeUsage))),
                        null);
                }
                else
                {
                    EntityType modelReturnParameterEntityType = modelReturnParameterStructuralType as EntityType;
                    if (modelReturnParameterEntityType != null)
                    {
                        storeReturnParameterRowType = RowType.Create(
                            modelReturnParameterEntityType.Properties.Select(property => property.Clone()),
                            null);
                    }
                    else
                    {
                        throw new NotSupportedException($"Structural type {modelReturnParameterStructuralType.FullName} of method {methodInfo.Name} cannot be converted to {nameof(RowType)}.");
                    }
                }

                return new FunctionParameter[]
                    {
                        FunctionParameter.Create(
                            "ReturnType",
                            storeReturnParameterRowType.GetCollectionType(), // Collection of RowType.
                            ParameterMode.ReturnValue)
                    };
            }

            if (functionAttribute.Type == FunctionType.NonComposableScalarValuedFunction)
            {
                // Non-composable scalar-valued function.
                return new FunctionParameter[0];
            }

            // Composable scalar-valued/Aggregate/Built in/Niladic function.
            // <Function Name="ufnGetProductListPrice" Aggregate="false" BuiltIn="false" NiladicFunction="false" IsComposable="true" ParameterTypeSemantics="AllowImplicitConversion" Schema="dbo" 
            //    ReturnType ="money">
            PrimitiveType storeReturnParameterPrimitiveType = model.GetStoreParameterPrimitiveType(methodInfo, returnParameterInfo, functionAttribute);
            return new FunctionParameter[]
                {
                    FunctionParameter.Create("ReturnType", storeReturnParameterPrimitiveType, ParameterMode.ReturnValue)
                };
        }

        private static IList<FunctionParameter> GetModelParameters(
            this DbModel model, MethodInfo methodInfo, EdmFunction storeFunction)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters().ToArray();
            return storeFunction
                .Parameters
                .Select((storeParameter, index) =>
                    {
                        ParameterInfo parameterInfo = parameters[index];
                        return FunctionParameter.Create(
                            parameterInfo.GetCustomAttribute<ParameterAttribute>()?.Name ?? parameterInfo.Name,
                            model.GetModelParameterPrimitiveType(methodInfo, parameterInfo),
                            storeParameter.Mode);
                    })
                .ToArray();
        }

        private static IList<FunctionParameter> GetModelParametersForModelDefinedFunction(
            this DbModel model, MethodInfo methodInfo)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters().ToArray();
            return parameters
                .Select((parameterInfo) => FunctionParameter.Create(
                    parameterInfo.GetCustomAttribute<ParameterAttribute>()?.Name ?? parameterInfo.Name,
                    model.GetModelStructualType(parameterInfo.ParameterType, methodInfo),
                    ParameterMode.In))
                .ToArray();
        }

        private static IList<FunctionParameter> GetModelReturnParameters(
            this DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
        {
            ParameterInfo returnParameterInfo = methodInfo.ReturnParameter;
            if (returnParameterInfo == null || returnParameterInfo.ParameterType == typeof(void))
            {
                throw new NotSupportedException($"The return parameter type of {methodInfo.Name} is not supported.");
            }

            ParameterAttribute returnParameterAttribute = returnParameterInfo.GetCustomAttribute<ParameterAttribute>();
            ResultTypeAttribute[] returnTypeAttributes = methodInfo.GetCustomAttributes<ResultTypeAttribute>().ToArray();
            IEnumerable<EdmType> modelReturnParameterEdmTypes;
            if (functionAttribute.Type == FunctionType.StoredProcedure)
            {
                if (returnParameterAttribute != null)
                {
                    throw new NotSupportedException(
                        $"{nameof(ParameterAttribute)} for method {methodInfo.Name} is not supported.");
                }

                modelReturnParameterEdmTypes = methodInfo
                    .GetStoredProcedureReturnTypes()
                    .Select(clrType => model.GetModelStructualType(clrType, methodInfo));
            }
            else
            {
                if (returnTypeAttributes.Any())
                {
                    throw new NotSupportedException(
                        $"{nameof(ResultTypeAttribute)} for method {methodInfo.Name} is not supported.");
                }

                if (functionAttribute.Type == FunctionType.TableValuedFunction)
                {
                    // returnParameterInfo.ParameterType is IQueryable<T>.
                    Type returnParameterClrType = returnParameterInfo.ParameterType.GetGenericArguments().Single();
                    StructuralType modelReturnParameterStructuralType = model.GetModelStructualType(returnParameterClrType, methodInfo);
                    modelReturnParameterEdmTypes = Enumerable.Repeat(modelReturnParameterStructuralType, 1);
                }
                else
                {
                    Type returnParameterClrType = returnParameterInfo.ParameterType;
                    Type returnParameterAttributeClrType = returnParameterAttribute?.ClrType;
                    if (returnParameterAttributeClrType != null
                        && returnParameterAttributeClrType != returnParameterClrType)
                    {
                        throw new NotSupportedException(
                            $"Return parameter of method {methodInfo.Name} is of {returnParameterClrType.FullName}, but its {nameof(ParameterAttribute)}.{nameof(ParameterAttribute.ClrType)} has a different type {returnParameterAttributeClrType.FullName}");
                    }

                    PrimitiveType returnParameterPrimitiveType = model.GetModelPrimitiveType(returnParameterClrType, methodInfo);
                    modelReturnParameterEdmTypes = Enumerable.Repeat(returnParameterPrimitiveType, 1);
                }
            }

            return modelReturnParameterEdmTypes
                .Select((edmType, index) => FunctionParameter.Create(
                    $"ReturnType{index}",
                    functionAttribute.Type == FunctionType.ModelDefinedFunction ? edmType : edmType.GetCollectionType(),
                    ParameterMode.ReturnValue))
                .ToArray();
        }

        private static PrimitiveType GetModelParameterPrimitiveType(
            this DbModel model, MethodInfo methodInfo, ParameterInfo parameterInfo)
        {
            // <Parameter Name="PersonID" Mode="In" Type="Int32" />
            Type parameterClrType = parameterInfo.ParameterType;
            ParameterAttribute parameterAttribute = parameterInfo.GetCustomAttribute<ParameterAttribute>();
            Type parameterAttributeClrType = parameterAttribute?.ClrType;
            if (parameterClrType == typeof(ObjectParameter))
            {
                // ObjectParameter.Type is available only when methodInfo is called.
                // When building model, its store type/clr type must be provided by ParameterAttribute.
                if (parameterAttributeClrType == null)
                {
                    throw new NotSupportedException(
                        $"Parameter {parameterInfo.Name} of method {methodInfo.Name} is not supported. {nameof(ObjectParameter)} parameter must have {nameof(ParameterAttribute)} with {nameof(ParameterAttribute.ClrType)} specified.");
                }

                parameterClrType = parameterAttributeClrType;
            }
            else
            {
                // When parameter is not ObjectParameter, ParameterAttribute.ClrType should be the same as parameterClrType, or not specified.
                if (parameterAttributeClrType != null && parameterAttributeClrType != parameterClrType)
                {
                    throw new NotSupportedException(
                        $"Parameter {parameterInfo.Name} of method {methodInfo.Name} if of {parameterClrType.FullName}, but its {nameof(ParameterAttribute)}.{nameof(ParameterAttribute.ClrType)} has a different type {parameterAttributeClrType.FullName}");
                }
            }

            return model.GetModelPrimitiveType(parameterClrType, methodInfo);
        }

        private static PrimitiveType GetModelPrimitiveType(this DbModel model, Type clrType, MethodInfo methodInfo)
        {
            // Parameter and return parameter can be Nullable<T>.
            // Return parameter can be IQueryable<T>, ObjectResult<T>.
            if (clrType.IsGenericType)
            {
                Type genericTypeDefinition = clrType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(Nullable<>)
                    || genericTypeDefinition == typeof(IQueryable<>)
                    || genericTypeDefinition == typeof(ObjectResult<>))
                {
                    clrType = clrType.GetGenericArguments().Single(); // Gets T from Nullable<T>.
                }
            }

            if (clrType.IsEnum)
            {
                EnumType modelEnumType = model
                    .ConceptualModel
                    .EnumTypes
                    .FirstOrDefault(enumType => enumType.FullName.EqualsOrdinal(clrType.FullName));
                if (modelEnumType == null)
                {
                    throw new NotSupportedException(
                        $"Enum type {nameof(clrType.FullName)} in method {methodInfo.Name} is not supported in conceptual model.");
                }

                return modelEnumType.UnderlyingType;
            }

            // clrType is not enum.
            PrimitiveType modelPrimitiveType = PrimitiveType
                .GetEdmPrimitiveTypes()
                .FirstOrDefault(primitiveType => primitiveType.ClrEquivalentType == clrType);
            if (modelPrimitiveType == null)
            {
                throw new NotSupportedException(
                    $"Type {nameof(clrType.FullName)} in method {methodInfo.Name} is not supported in conceptual model.");
            }

            return modelPrimitiveType;
        }

        private static StructuralType GetModelStructualType(this DbModel model, Type clrType, MethodInfo methodInfo)
        {
            EntityType modelEntityType = model.GetModelEntityType(clrType, methodInfo);
            if (modelEntityType != null)
            {
                return modelEntityType;
            }

            ComplexType complexType = model.GetModelComplexType(clrType, methodInfo);
            if (complexType != null)
            {
                return complexType;
            }

            throw new NotSupportedException(
                $"{clrType.FullName} for method {methodInfo.Name} is not supported in conceptual model as a structural type.  This can be caused by a failure to register this type as complex. For more information, see https://msdn.microsoft.com/en-us/library/gg679474.aspx")
            {
                HelpLink = "https://msdn.microsoft.com/en-us/library/gg679474.aspx"
            };
        }

        private static EntityType GetModelEntityType(this DbModel model, Type clrType, MethodInfo methodInfo)
        {
            PropertyInfo[] clrProperties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            EntityType[] entityTypes = model
                .ConceptualModel
                .EntityTypes
                .Where(entityType =>
                    entityType.FullName.EqualsOrdinal(clrType.FullName)
                    || entityType.Name.EqualsOrdinal(clrType.Name)
                    && entityType
                        .Properties
                        .All(edmProperty => clrProperties
                            .Any(clrProperty =>
                            {
                                if (!edmProperty.Name.EqualsOrdinal(clrProperty.Name))
                                {
                                    return false;
                                }

                                // Entity type's property can be either primitive type or another complex type.
                                if (edmProperty.PrimitiveType != null)
                                {
                                    // Entity type's property is primitive type.
                                    return edmProperty.PrimitiveType.ClrEquivalentType == (Nullable.GetUnderlyingType(clrProperty.PropertyType) ?? clrProperty.PropertyType);
                                }

                                if (edmProperty.ComplexType != null)
                                {
                                    // Entity type's property is complex type.
                                    return edmProperty.ComplexType.Name.EqualsOrdinal(clrProperty.PropertyType.Name);
                                }

                                return false;
                            })))
                .ToArray();

            if (entityTypes.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{clrType.FullName} for method {methodInfo.Name} has multiple ambiguous matching entity types in conceptual model: {string.Join(", ", entityTypes.Select(entityType => entityType.FullName))}.");
            }

            return entityTypes.SingleOrDefault();
        }

        private static ComplexType GetModelComplexType(this DbModel model, Type clrType, MethodInfo methodInfo)
        {
            // Cannot add missing complex type instantly. The following code does not work.
            // if (Attribute.IsDefined(clrType, typeof(ComplexTypeAttribute)))
            // {
            //    MethodInfo complexTypeMethod = typeof(DbModelBuilder).GetMethod(nameof(modelBuilder.ComplexType));
            //    complexTypeMethod.MakeGenericMethod(clrType).Invoke(modelBuilder, null);
            //    model.Compile();
            //    modelStructualType = model
            //        .ConceptualModel
            //        .ComplexTypes
            //        .FirstOrDefault(complexType => complexType.FullName.EqualsOrdinal(clrType.FullName));
            // }

            PropertyInfo[] clrProperties = clrType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty);
            ComplexType[] modelComplexTypes = model
                .ConceptualModel
                .ComplexTypes
                .Where(complexType =>
                    complexType.FullName.EqualsOrdinal(clrType.FullName)
                    || complexType.Name.EqualsOrdinal(clrType.Name)
                    && complexType
                        .Properties
                        .All(edmProperty => clrProperties
                            .Any(clrProperty =>
                            {
                                if (!edmProperty.Name.EqualsOrdinal(clrProperty.Name))
                                {
                                    return false;
                                }

                                // Complex type's property can be either primitive type or another complex type.
                                if (edmProperty.PrimitiveType != null)
                                {
                                    // Complex type's property is primitive type.
                                    return edmProperty.PrimitiveType.ClrEquivalentType == (Nullable.GetUnderlyingType(clrProperty.PropertyType) ?? clrProperty.PropertyType);
                                }

                                if (edmProperty.ComplexType != null)
                                {
                                    // Complex type's property is complex type.
                                    return edmProperty.ComplexType.Name.EqualsOrdinal(clrProperty.PropertyType.Name);
                                }

                                return false;
                            })))
                .ToArray();

            if (modelComplexTypes.Length > 1)
            {
                throw new InvalidOperationException(
                    $"{clrType.FullName} for method {methodInfo.Name} has multiple ambiguous matching complex types in conceptual model: {string.Join(", ", modelComplexTypes.Select(complexType => complexType.FullName))}.");
            }

            return modelComplexTypes.SingleOrDefault();
        }

        private static IList<EntitySet> GetModelEntitySets(this DbModel model, MethodInfo methodInfo, FunctionAttribute functionAttribute)
        {
            ParameterInfo returnParameterInfo = methodInfo.ReturnParameter;
            if (returnParameterInfo == null || returnParameterInfo.ParameterType == typeof(void))
            {
                throw new NotSupportedException($"The return parameter type of {methodInfo.Name} is not supported.");
            }

            if (functionAttribute.Type == FunctionType.StoredProcedure && returnParameterInfo.ParameterType != typeof(int))
            {
                // returnParameterInfo.ParameterType is ObjectResult<T>.
                Type[] returnParameterClrTypes = methodInfo.GetStoredProcedureReturnTypes().ToArray();
                if (returnParameterClrTypes.Length > 1)
                {
                    // Stored procedure has more than one result. 
                    // EdmFunctionPayload.EntitySets must be provided. Otherwise, an ArgumentException will be thrown:
                    // The EntitySets parameter must not be null for functions that return multiple result sets.
                    return returnParameterClrTypes.Select(clrType =>
                    {
                        EntitySet modelEntitySet = model
                            .ConceptualModel
                            .Container
                            .EntitySets
                            .FirstOrDefault(entitySet => entitySet.ElementType == model.GetModelEntityType(clrType, methodInfo)); // TODO: bug.
                        if (modelEntitySet == null)
                        {
                            throw new NotSupportedException(
                                $"{clrType.FullName} for method {methodInfo.Name} is not supported in conceptual model as entity set.");
                        }

                        return modelEntitySet;
                    }).ToArray();
                }
            }
            else if (functionAttribute.Type == FunctionType.TableValuedFunction)
            {
                // returnParameterInfo.ParameterType is IQueryable<T>.
                Type returnParameterClrType = returnParameterInfo.ParameterType.GetGenericArguments().Single();
                EntityType returnParameterEntityType = model.GetModelEntityType(returnParameterClrType, methodInfo);
                if (returnParameterEntityType != null)
                {
                    EntitySet modelEntitySet = model
                        .ConceptualModel
                        .Container
                        .EntitySets
                        .FirstOrDefault(entitySet => entitySet.ElementType == returnParameterEntityType);
                    if (modelEntitySet == null)
                    {
                        throw new NotSupportedException(
                            $"{returnParameterInfo.ParameterType.FullName} for method {methodInfo.Name} is not supported in conceptual model as entity set.");
                    }

                    return new EntitySet[] { modelEntitySet };
                }
            }

            // Do not return new EntitySet[0], which causes a ArgumentException:
            // The number of entity sets should match the number of return parameters.
            return null;
        }
    }
}
