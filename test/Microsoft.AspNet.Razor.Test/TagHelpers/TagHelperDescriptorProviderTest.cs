﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperDescriptorProviderTest
    {
        public static TheoryData RequiredAttributeData
        {
            get
            {
                var divDescriptor = new TagHelperDescriptor(
                    tagName: "div",
                    typeName: "DivTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                    requiredAttributes: new[] { "style" });
                var inputDescriptor = new TagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                    requiredAttributes: new[] { "class", "style" });
                var catchAllDescriptor = new TagHelperDescriptor(
                    tagName: "*",
                    typeName: "CatchAllTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                    requiredAttributes: new[] { "class" });
                var catchAllDescriptor2 = new TagHelperDescriptor(
                    tagName: "*",
                    typeName: "CatchAllTagHelper",
                    assemblyName: "SomeAssembly",
                    attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                    requiredAttributes: new[] { "custom", "class" });
                var defaultAvailableDescriptors =
                    new[] { divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2 };

                return new TheoryData<
                    string, // tagName
                    IEnumerable<string>, // providedAttributes
                    IEnumerable<TagHelperDescriptor>, // expectedDescriptors
                    IEnumerable<TagHelperDescriptor>> // availableDescriptors
                {
                    { "div", new[] { "custom" }, new TagHelperDescriptor[0],  defaultAvailableDescriptors },
                    { "div", new[] { "style" }, new[] { divDescriptor },  defaultAvailableDescriptors },
                    { "div", new[] { "class" }, new[] { catchAllDescriptor },  defaultAvailableDescriptors },
                    {
                        "div",
                        new[] { "class", "style" },
                        new[] { divDescriptor, catchAllDescriptor },
                        defaultAvailableDescriptors
                    },
                    {
                        "div",
                        new[] { "class", "style", "custom" },
                        new[] { divDescriptor, catchAllDescriptor, catchAllDescriptor2 },
                        defaultAvailableDescriptors
                    },
                    {
                        "input",
                        new[] { "class", "style" },
                        new[] { inputDescriptor, catchAllDescriptor },
                        defaultAvailableDescriptors
                    },
                    { "*", new[] { "custom" }, new TagHelperDescriptor[0],  defaultAvailableDescriptors },
                    { "*", new[] { "class" }, new[] { catchAllDescriptor },  defaultAvailableDescriptors },
                    { "*", new[] { "class", "style" }, new[] { catchAllDescriptor }, defaultAvailableDescriptors },
                    {
                        "*",
                        new[] { "class", "custom" },
                        new[] { catchAllDescriptor, catchAllDescriptor2 },
                        defaultAvailableDescriptors
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredAttributeData))]
        public void GetDescriptors_ReturnsDescriptorsWithRequiredAttributes(
            string tagName,
            IEnumerable<string> providedAttributes,
            IEnumerable<TagHelperDescriptor> expectedDescriptors,
            IEnumerable<TagHelperDescriptor> availableDescriptors)
        {
            // Arrange
            var provider = new TagHelperDescriptorProvider(availableDescriptors);

            // Act
            var resolvedDescriptors = provider.GetDescriptors(tagName, providedAttributes);

            // Assert
            Assert.Equal(expectedDescriptors, resolvedDescriptors, TagHelperDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_ReturnsEmptyDescriptorsWithPrefixAsTagName()
        {
            // Arrange
            var catchAllDescriptor = CreatePrefixedDescriptor("th", "*", "foo1");
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var resolvedDescriptors = provider.GetDescriptors("th", attributeNames: Enumerable.Empty<string>());

            // Assert
            Assert.Empty(resolvedDescriptors);
        }

        [Fact]
        public void GetDescriptors_OnlyUnderstandsSinglePrefix()
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", "div", "foo1");
            var spanDescriptor = CreatePrefixedDescriptor("th2:", "span", "foo2");
            var descriptors = new[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetDescriptors("th:div", attributeNames: Enumerable.Empty<string>());
            var retrievedDescriptorsSpan = provider.GetDescriptors("th2:span", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptorsDiv);
            Assert.Same(divDescriptor, descriptor);
            Assert.Empty(retrievedDescriptorsSpan);
        }

        [Fact]
        public void GetDescriptors_ReturnsCatchAllDescriptorsForPrefixedTags()
        {
            // Arrange
            var catchAllDescriptor = CreatePrefixedDescriptor("th:", "*", "foo1");
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetDescriptors("th:div", attributeNames: Enumerable.Empty<string>());
            var retrievedDescriptorsSpan = provider.GetDescriptors("th:span", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptorsDiv);
            Assert.Same(catchAllDescriptor, descriptor);
            descriptor = Assert.Single(retrievedDescriptorsSpan);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetDescriptors_ReturnsDescriptorsForPrefixedTags()
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", "div", "foo1");
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("th:div", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("div")]
        public void GetDescriptors_ReturnsNothingForUnprefixedTags(string tagName)
        {
            // Arrange
            var divDescriptor = CreatePrefixedDescriptor("th:", tagName, "foo1");
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptorsDiv = provider.GetDescriptors("div", attributeNames: Enumerable.Empty<string>());

            // Assert
            Assert.Empty(retrievedDescriptorsDiv);
        }

        [Fact]
        public void GetDescriptors_ReturnsNothingForUnregisteredTags()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var spanDescriptor = new TagHelperDescriptor("span", "foo2", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("foo", attributeNames: Enumerable.Empty<string>());

            // Assert
            Assert.Empty(retrievedDescriptors);
        }

        [Fact]
        public void GetDescriptors_DoesNotReturnNonCatchAllTagsForCatchAll()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var spanDescriptor = new TagHelperDescriptor("span", "foo2", "SomeAssembly");
            var catchAllDescriptor = new TagHelperDescriptor("*", "foo3", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("*", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetDescriptors_ReturnsCatchAllsWithEveryTagName()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var spanDescriptor = new TagHelperDescriptor("span", "foo2", "SomeAssembly");
            var catchAllDescriptor = new TagHelperDescriptor("*", "foo3", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var divDescriptors = provider.GetDescriptors("div", attributeNames: Enumerable.Empty<string>());
            var spanDescriptors = provider.GetDescriptors("span", attributeNames: Enumerable.Empty<string>());

            // Assert
            // For divs
            Assert.Equal(2, divDescriptors.Count());
            Assert.Contains(divDescriptor, divDescriptors);
            Assert.Contains(catchAllDescriptor, divDescriptors);

            // For spans
            Assert.Equal(2, spanDescriptors.Count());
            Assert.Contains(spanDescriptor, spanDescriptors);
            Assert.Contains(catchAllDescriptor, spanDescriptors);
        }

        [Fact]
        public void GetDescriptors_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
        {
            // Arrange
            var divDescriptor = new TagHelperDescriptor("div", "foo1", "SomeAssembly");
            var descriptors = new TagHelperDescriptor[] { divDescriptor, divDescriptor };
            var provider = new TagHelperDescriptorProvider(descriptors);

            // Act
            var retrievedDescriptors = provider.GetDescriptors("div", attributeNames: Enumerable.Empty<string>());

            // Assert
            var descriptor = Assert.Single(retrievedDescriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        private static TagHelperDescriptor CreatePrefixedDescriptor(string prefix, string tagName, string typeName)
        {
            return new TagHelperDescriptor(
                prefix, 
                tagName, 
                typeName, 
                assemblyName: "SomeAssembly", 
                attributes: Enumerable.Empty<TagHelperAttributeDescriptor>(),
                requiredAttributes: Enumerable.Empty<string>());
        }
    }
}