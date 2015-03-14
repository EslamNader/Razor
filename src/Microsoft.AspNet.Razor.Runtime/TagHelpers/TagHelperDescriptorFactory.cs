// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Factory for <see cref="TagHelperDescriptor"/>s from <see cref="Type"/>s.
    /// </summary>
    public static class TagHelperDescriptorFactory
    {
        private const string TagHelperNameEnding = "TagHelper";
        private const string HtmlCaseRegexReplacement = "-$1$2";

        // This matches the following AFTER the start of the input string (MATCH).
        // Any letter/number followed by an uppercase letter then lowercase letter: 1(Aa), a(Aa), A(Aa)
        // Any lowercase letter followed by an uppercase letter: a(A)
        // Each match is then prefixed by a "-" via the ToHtmlCase method.
        private static readonly Regex HtmlCaseRegex =
            new Regex("(?<!^)((?<=[a-zA-Z0-9])[A-Z][a-z])|((?<=[a-z])[A-Z])", RegexOptions.None);

        // TODO: Investigate if we should cache TagHelperDescriptors for types:
        // https://github.com/aspnet/Razor/issues/165

        /// <summary>
        /// Creates a <see cref="TagHelperDescriptor"/> from the given <paramref name="type"/>.
        /// </summary>
        /// <param name="assemblyName">The assembly name that contains <paramref name="type"/>.</param>
        /// <param name="type">The type to create a <see cref="TagHelperDescriptor"/> from.</param>
        /// <returns>A <see cref="TagHelperDescriptor"/> that describes the given <paramref name="type"/>.</returns>
        public static IEnumerable<TagHelperDescriptor> CreateDescriptors(
            string assemblyName,
            Type type,
            ParserErrorSink errorSink)
        {
            var typeInfo = type.GetTypeInfo();
            var attributeDescriptors = GetAttributeDescriptors(type);
            var targetElementAttributes = GetValidTargetElementAttributes(typeInfo, errorSink);
            var tagHelperDescriptors =
                BuildTagHelperDescriptors(
                    typeInfo,
                    assemblyName,
                    attributeDescriptors,
                    targetElementAttributes);

            return tagHelperDescriptors.Distinct(TagHelperDescriptorComparer.Default);
        }

        public static ICollection<char> InvalidNonWhitespaceNameCharacters { get; } = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'' });

        private static IEnumerable<TargetElementAttribute> GetValidTargetElementAttributes(
            TypeInfo typeInfo,
            ParserErrorSink errorSink)
        {
            var targetElementAttributes = typeInfo.GetCustomAttributes<TargetElementAttribute>(inherit: false);

            return targetElementAttributes.Where(attribute => ValidTargetElementAttributeNames(attribute, errorSink));
        }

        private static IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            TypeInfo typeInfo,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<TargetElementAttribute> targetElementAttributes)
        {
            var typeName = typeInfo.FullName;

            // If there isn't an attribute specifying the tag name derive it from the name
            if (!targetElementAttributes.Any())
            {
                var name = typeInfo.Name;

                if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - TagHelperNameEnding.Length);
                }

                return BuildTagHelperDescriptors(
                    tagNames: new[] { ToHtmlCase(name) },
                    typeName: typeName,
                    assemblyName: assemblyName,
                    attributeDescriptors: attributeDescriptors,
                    requiredAttributes: Enumerable.Empty<string>());
            }

            return targetElementAttributes.SelectMany(
                attribute => BuildTagHelperDescriptors(typeName, assemblyName, attributeDescriptors, attribute));
        }

        private static IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            TargetElementAttribute targetElementAttribute)
        {
            var tagNames = GetCommaSeparatedValues(targetElementAttribute.Tags);
            var requiredAttributes = GetCommaSeparatedValues(targetElementAttribute.Attributes);

            return BuildTagHelperDescriptors(
                tagNames,
                typeName,
                assemblyName,
                attributeDescriptors,
                requiredAttributes);
        }

        private static IEnumerable<TagHelperDescriptor> BuildTagHelperDescriptors(
            IEnumerable<string> tagNames,
            string typeName,
            string assemblyName,
            IEnumerable<TagHelperAttributeDescriptor> attributeDescriptors,
            IEnumerable<string> requiredAttributes)
        {
            return tagNames.Select(tagName =>
                new TagHelperDescriptor(
                    prefix: string.Empty,
                    tagName: tagName,
                    typeName: typeName,
                    assemblyName: assemblyName,
                    attributes: attributeDescriptors,
                    requiredAttributes: requiredAttributes));
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal static IEnumerable<string> GetCommaSeparatedValues(string text)
        {
            // We don't want to remove empty entries, need to notify users of invalid values.
            return text?.Split(',').Select(tagName => tagName.Trim()) ?? Enumerable.Empty<string>();
        }

        /// <summary>
        /// Internal for testing.
        /// </summary>
        internal static bool ValidTargetElementAttributeNames(
            TargetElementAttribute attribute,
            ParserErrorSink errorSink)
        {
            return
                ValidateNames(
                    names: GetCommaSeparatedValues(attribute.Tags),
                    targetingAttributes: false,
                    errorSink: errorSink) &&
                ValidateNames(
                    names: GetCommaSeparatedValues(attribute.Attributes),
                    targetingAttributes: true,
                    errorSink: errorSink);

        }

        private static bool ValidateNames(
            IEnumerable<string> names,
            bool targetingAttributes,
            ParserErrorSink errorSink)
        {
            var targetName = targetingAttributes ?
                Resources.TagHelperDescriptorFactory_Attribute :
                Resources.TagHelperDescriptorFactory_Tag;
            var validNames = true;

            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        Resources.FormatTargetElementAttribute_NameCannotBeNullOrWhitespace(targetName));

                    validNames = false;

                    continue;
                }

                foreach (var character in name)
                {
                    if (char.IsWhiteSpace(character) ||
                        InvalidNonWhitespaceNameCharacters.Contains(character))
                    {
                        errorSink.OnError(
                            SourceLocation.Zero,
                            Resources.FormatTargetElementAttribute_InvalidName(
                                targetName.ToLowerInvariant(),
                                name,
                                character));

                        validNames = false;
                    }
                }
            }

            return validNames;
        }

        private static IEnumerable<TagHelperAttributeDescriptor> GetAttributeDescriptors(Type type)
        {
            var properties = type.GetRuntimeProperties().Where(IsValidProperty);
            var attributeDescriptors = properties.Select(ToAttributeDescriptor);

            return attributeDescriptors;
        }

        private static TagHelperAttributeDescriptor ToAttributeDescriptor(PropertyInfo property)
        {
            var attributeNameAttribute = property.GetCustomAttribute<HtmlAttributeNameAttribute>(inherit: false);
            var attributeName = attributeNameAttribute != null ?
                                attributeNameAttribute.Name :
                                ToHtmlCase(property.Name);

            return new TagHelperAttributeDescriptor(attributeName, property.Name, property.PropertyType.FullName);
        }

        private static bool IsValidProperty(PropertyInfo property)
        {
            return property.GetMethod != null &&
                   property.GetMethod.IsPublic &&
                   property.SetMethod != null &&
                   property.SetMethod.IsPublic;
        }

        /// <summary>
        /// Converts from pascal/camel case to lower kebab-case.
        /// </summary>
        /// <example>
        /// SomeThing => some-thing
        /// capsONInside => caps-on-inside
        /// CAPSOnOUTSIDE => caps-on-outside
        /// ALLCAPS => allcaps
        /// One1Two2Three3 => one1-two2-three3
        /// ONE1TWO2THREE3 => one1two2three3
        /// First_Second_ThirdHi => first_second_third-hi
        /// </example>
        private static string ToHtmlCase(string name)
        {
            return HtmlCaseRegex.Replace(name, HtmlCaseRegexReplacement).ToLowerInvariant();
        }
    }
}