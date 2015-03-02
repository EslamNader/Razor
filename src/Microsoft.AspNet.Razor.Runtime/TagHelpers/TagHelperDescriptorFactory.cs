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
        public static readonly ISet<char> InvalidNonWhitespaceNameCharacters = new HashSet<char>(
            new[] { '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'' });

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
            var elementTargets = GetElementTargets(type, errorSink);
            var typeName = type.FullName;
            var attributeDescriptors = GetAttributeDescriptors(type);

            return elementTargets.Select(
                elementTarget =>
                    new TagHelperDescriptor(
                        prefix: string.Empty,
                        tagName: elementTarget.TagName,
                        typeName: typeName,
                        assemblyName: assemblyName,
                        attributes: attributeDescriptors,
                        requiredAttributes: elementTarget.AttributeNames));
        }

        private static IEnumerable<ElementTarget> GetElementTargets(Type tagHelperType, ParserErrorSink errorSink)
        {
            var typeInfo = tagHelperType.GetTypeInfo();
            var targetElementAttributes = typeInfo.GetCustomAttributes<TargetElementAttribute>(inherit: false);

            // If there isn't an attribute specifying the tag name derive it from the name
            if (!targetElementAttributes.Any())
            {
                var name = typeInfo.Name;

                if (name.EndsWith(TagHelperNameEnding, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - TagHelperNameEnding.Length);
                }

                return new[]
                {
                    new ElementTarget
                    {
                        TagName = ToHtmlCase(name),
                        AttributeNames = Enumerable.Empty<string>()
                    }
                };
            }

            return targetElementAttributes.SelectMany(
                targetElementAttribute =>
                {
                    IEnumerable<string> tagNames;
                    IEnumerable<string> attributeNames = null;

                    if (!TryGetValidatedNames(
                            targetElementAttribute.Tags,
                            pascalNameTarget: "Tag",
                            errorSink: errorSink,
                            names: out tagNames) ||
                        (targetElementAttribute.Attributes != null &&
                        !TryGetValidatedNames(
                            targetElementAttribute.Attributes,
                            pascalNameTarget: "Attribute",
                            errorSink: errorSink,
                            names: out attributeNames)))
                    {
                        return Enumerable.Empty<ElementTarget>();
                    }

                    return BuildElementTargets(tagNames, attributeNames, tagHelperType);
                }).ToArray();
        }

        // Internal for testing
        internal static bool TryGetValidatedNames(
            string commaSeparatedNames,
            string pascalNameTarget,
            ParserErrorSink errorSink,
            out IEnumerable<string> names)
        {
            names = GetCommaSeparatedValues(commaSeparatedNames)?.Distinct();

            return ValidateNames(names, pascalNameTarget, errorSink);
        }

        private static IEnumerable<string> GetCommaSeparatedValues(string text)
        {
            // We don't want to remove empty entries, need to notify users of invalid values.
            return text.Split(',').Select(tagName => tagName.Trim());
        }

        private static bool ValidateNames(
            IEnumerable<string> names,
            string pascalNameTarget,
            ParserErrorSink errorSink)
        {
            foreach (var name in names)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    errorSink.OnError(
                        SourceLocation.Zero,
                        Resources.FormatTargetElementAttribute_NameCannotBeNullOrWhitespace(pascalNameTarget));

                    return false;
                }

                foreach (var character in name)
                {
                    if (InvalidNonWhitespaceNameCharacters.Contains(character))
                    {
                        errorSink.OnError(
                            SourceLocation.Zero,
                            Resources.FormatTargetElementAttribute_InvalidName(
                                pascalNameTarget.ToLowerInvariant(),
                                name,
                                character));

                        return false;
                    }
                }
            }

            return true;
        }

        private static IEnumerable<ElementTarget> BuildElementTargets(
            IEnumerable<string> tagNames,
            IEnumerable<string> attributeNames,
            Type tagHelperType)
        {
            return tagNames.Select(tagName =>
                new ElementTarget
                {
                    TagName = tagName,
                    AttributeNames = attributeNames ?? Enumerable.Empty<string>()
                });
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

        private class ElementTarget
        {
            public string TagName { get; set; }
            public IEnumerable<string> AttributeNames { get; set; }
        }
    }
}