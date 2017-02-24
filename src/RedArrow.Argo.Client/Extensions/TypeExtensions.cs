﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RedArrow.Argo.Attributes;
using RedArrow.Argo.Client.Config.Model;
using RedArrow.Argo.Client.Exceptions;
using RedArrow.Argo.Model;

namespace RedArrow.Argo.Client.Extensions
{
    internal static class TypeExtensions
    {
        internal static ConstructorInfo GetDefaultConstructor(this Type type)
        {
            if (type == null || type.GetTypeInfo().IsAbstract)
            {
                return null;
            }

            var result = type.GetTypeInfo()
                .DeclaredConstructors
                .FirstOrDefault(ctor => ctor.IsPublic && !ctor.GetParameters().Any());

            if (result == null)
            {
                throw new ArgoException("A default (no-arg) constructor could not be found for: ", type);
            }

            return result;
        }

        internal static string GetModelResourceType(this Type type)
        {
            return type.GetTypeInfo()
                .CustomAttributes
                .Single(a => a.AttributeType == typeof(ModelAttribute))
                .ConstructorArguments
                .Select(arg => arg.Value as string)
                .FirstOrDefault() ?? type.Name.Camelize();
        }

        internal static PropertyInfo GetModelResourceProperty(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .Where(prop => prop.Name == "__argo__generated_Resource")
                .Single(prop => prop.PropertyType == typeof(IResourceIdentifier));
        }

        internal static PropertyInfo GetModelPatchProperty(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .Where(prop => prop.Name == "__argo__generated_Patch")
                .Single(prop => prop.PropertyType == typeof(IResourceIdentifier));
        }

        internal static PropertyInfo GetModelIdProperty(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .Single(prop => prop.IsDefined(typeof(IdAttribute)));
        }

        internal static IDictionary<string, AttributeConfiguration> GetModelAttributeConfigurations(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .Where(prop => prop.IsDefined(typeof(PropertyAttribute)))
                .Select(prop => new AttributeConfiguration(prop))
                .ToDictionary(
                    attrConfig => attrConfig.AttributeName,
                    attrConfig => attrConfig);
        }

        internal static IDictionary<string, HasOneConfiguration> GetModelHasOneConfigurations(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .Where(prop => prop.IsDefined(typeof(HasOneAttribute)))
                .Select(prop => new HasOneConfiguration(prop))
                .ToDictionary(
                    has1Cfg => has1Cfg.RelationshipName,
                    has1Cfg => has1Cfg);
        }

        internal static IDictionary<string, HasManyConfiguration> GetModelHasManyConfigurations(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .Where(prop => prop.IsDefined(typeof(HasManyAttribute)))
                .Select(prop => new HasManyConfiguration(prop))
                .ToDictionary(
                    hasMCfg => hasMCfg.RelationshipName,
                    hasMCfg => hasMCfg);
        }

        internal static PropertyInfo GetPropertyBagProperty(this Type type)
        {
            return type.GetTypeInfo()
                .DeclaredProperties
                .SingleOrDefault(prop => prop.IsDefined(typeof(PropertyBagAttribute)));
        }
    }
}