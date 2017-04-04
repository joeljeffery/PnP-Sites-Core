﻿using OfficeDevPnP.Core.Framework.Provisioning.Model;
using OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Resolvers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OfficeDevPnP.Core.Framework.Provisioning.Providers.Xml.Serializers
{
    /// <summary>
    /// Class to serialize/deserialize the Base Properties of a Template
    /// </summary>
    [TemplateSchemaSerializer(SerializationSequence = 100, DeserializationSequence = 100,
        MinimalSupportedSchemaVersion = XMLPnPSchemaVersion.V201605,
        Default = true)]
    internal class TemplateBasePropertiesSerializer : PnPBaseSchemaSerializer<ProvisioningTemplate>
    {
        public override void Deserialize(object persistence, ProvisioningTemplate template)
        {
            // Define the dynamic type for the template's properties
            var propertiesTypeName = $"{PnPSerializationScope.Current?.BaseSchemaNamespace}.StringDictionaryItem, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
            var propertiesType = Type.GetType(propertiesTypeName, true);
            var propertiesKeySelector = CreateSelectorLambda(propertiesType, "Key");
            var propertiesValueSelector = CreateSelectorLambda(propertiesType, "Value");

            // TODO: Find a way to avoid creating the dictionary of IResolver objects manually
            var expressions = new Dictionary<Expression<Func<ProvisioningTemplate, Object>>, IResolver>();
            expressions.Add(t => t.Version, new FromDecimalToDoubleValueResolver());
            expressions.Add(t => t.Properties,
                new FromArrayToDictionaryValueResolver<String, String>(
                    propertiesType, propertiesKeySelector, propertiesValueSelector));

            //search settings
            expressions.Add(t => t.SiteSearchSettings,
                new ExpressionValueResolver((s, v) =>
                s.GetPublicInstancePropertyValue("SearchSettings")?
                .GetPublicInstancePropertyValue("SiteSearchSettings")?
                .GetPublicInstancePropertyValue("OuterXml")));
            expressions.Add(t => t.WebSearchSettings,
                new ExpressionValueResolver((s, v) =>
                s.GetPublicInstancePropertyValue("SearchSettings")?
                .GetPublicInstancePropertyValue("WebSearchSettings")?
                .GetPublicInstancePropertyValue("OuterXml")));

            PnPObjectsMapper.MapProperties(persistence, template, expressions);
        }

        public override void Serialize(ProvisioningTemplate template, object persistence)
        {
            var propertiesTypeName = $"{PnPSerializationScope.Current?.BaseSchemaNamespace}.StringDictionaryItem, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}";
            var propertiesType = Type.GetType(propertiesTypeName, true);

            var keySelector = CreateSelectorLambda(propertiesType, "Key");
            var valueSelector = CreateSelectorLambda(propertiesType, "Value");

            var expressions = new Dictionary<Expression<Func<ProvisioningTemplate, Object>>, IResolver>();
            expressions.Add(t => t.Version, new FromDoubleToDecimalValueResolver());
            expressions.Add(t => t.Properties,
                new FromDictionaryToArrayValueResolver<String, String>(
                    propertiesType, keySelector, valueSelector));

            PnPObjectsMapper.MapProperties(template, persistence, expressions);

            //search settings
            if(!string.IsNullOrEmpty(template.SiteSearchSettings)||!string.IsNullOrEmpty(template.WebSearchSettings))
            {
                var searchSettingType = Type.GetType($"{PnPSerializationScope.Current?.BaseSchemaNamespace}.ProvisioningTemplateSearchSettings, {PnPSerializationScope.Current?.BaseSchemaAssemblyName}", true);
                var searchSettings = Activator.CreateInstance(searchSettingType, true);
                if(!string.IsNullOrEmpty(template.SiteSearchSettings))
                {
                    searchSettings.GetPublicInstanceProperty("SiteSearchSettings").SetValue(searchSettings, XElement.Parse(template.SiteSearchSettings).ToXmlElement());
                }
                if (!string.IsNullOrEmpty(template.WebSearchSettings))
                {
                    searchSettings.GetPublicInstanceProperty("WebSearchSettings").SetValue(searchSettings, XElement.Parse(template.WebSearchSettings).ToXmlElement());
                }
                persistence.GetPublicInstanceProperty("SearchSettings").SetValue(persistence, searchSettings);
            }

        }
    }
}