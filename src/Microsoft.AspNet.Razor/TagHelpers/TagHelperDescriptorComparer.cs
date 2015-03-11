﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    /// <summary>
    /// An <see cref="IEqualityComparer{TagHelperDescriptor}"/> used to check equality between
    /// two <see cref="TagHelperDescriptor"/>s.
    /// </summary>
    public class TagHelperDescriptorComparer : IEqualityComparer<TagHelperDescriptor>
    {
        /// <summary>
        /// A default instance of the <see cref="TagHelperDescriptorComparer"/>.
        /// </summary>
        public static readonly TagHelperDescriptorComparer Default = new TagHelperDescriptorComparer();

        /// <summary>
        /// Determines if the two given tag helpers are equal.
        /// </summary>
        /// <param name="descriptorX">A <see cref="TagHelperDescriptor"/> to compare with the given
        /// <paramref name="descriptorY"/>.</param>
        /// <param name="descriptorY">A <see cref="TagHelperDescriptor"/> to compare with the given
        /// <paramref name="descriptorX"/>.</param>
        /// <returns><c>true</c> if <paramref name="descriptorX"/> and <paramref name="descriptorY"/> are equal,
        /// <c>false</c> otherwise.</returns>
        /// <remarks>
        /// Determines equality based on <see cref="TagHelperDescriptor.TypeName"/>,
        /// <see cref="TagHelperDescriptor.AssemblyName"/>, <see cref="TagHelperDescriptor.TagName"/>,
        /// <see cref="TagHelperDescriptor.Prefix"/>, <see cref="TagHelperDescriptor.Attributes"/>, and 
        /// <see cref="TagHelperDescriptor.RequiredAttributes"/>.
        /// </remarks>
        public bool Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            return string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal) &&
                   string.Equals(descriptorX.TagName, descriptorY.TagName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(descriptorX.Prefix, descriptorY.Prefix, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(descriptorX.AssemblyName, descriptorY.AssemblyName, StringComparison.Ordinal) &&
                   Enumerable.SequenceEqual(
                       descriptorX.RequiredAttributes.OrderBy(
                           attribute => attribute, StringComparer.OrdinalIgnoreCase),
                       descriptorY.RequiredAttributes.OrderBy(
                           attribute => attribute, StringComparer.OrdinalIgnoreCase),
                       StringComparer.OrdinalIgnoreCase) &&
                   Enumerable.SequenceEqual(
                       descriptorX.Attributes.OrderBy(
                           attribute => TagHelperAttributeDescriptorComparer.Default.GetHashCode(attribute)),
                       descriptorY.Attributes.OrderBy(
                           attribute => TagHelperAttributeDescriptorComparer.Default.GetHashCode(attribute)),
                       TagHelperAttributeDescriptorComparer.Default);
        }

        /// <summary>
        /// Returns an <see cref="int"/> value that uniquely identifies the given <see cref="TagHelperDescriptor"/>.
        /// </summary>
        /// <param name="descriptor">The <see cref="TagHelperDescriptor"/> to create a hash code for.</param>
        /// <returns>An <see cref="int"/> that uniquely identifies the given <paramref name="descriptor"/>.</returns>
        public int GetHashCode(TagHelperDescriptor descriptor)
        {
            return HashCodeCombiner
                .Start()
                .Add(descriptor.TagName, StringComparer.OrdinalIgnoreCase)
                .Add(descriptor.TypeName, StringComparer.Ordinal)
                .Add(descriptor.AssemblyName, StringComparer.Ordinal)
                .Add(descriptor.RequiredAttributes)
                .CombinedHash;
        }

        private class TagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
        {
            public static readonly TagHelperAttributeDescriptorComparer Default =
                new TagHelperAttributeDescriptorComparer();

            private TagHelperAttributeDescriptorComparer()
            {
            }

            public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
            {
                return string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                       string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
            }

            public int GetHashCode(TagHelperAttributeDescriptor descriptor)
            {
                return HashCodeCombiner
                    .Start()
                    .Add(descriptor.Name, StringComparer.OrdinalIgnoreCase)
                    .Add(descriptor.PropertyName, StringComparer.Ordinal)
                    .Add(descriptor.TypeName, StringComparer.Ordinal)
                    .CombinedHash;
            }
        }
    }
}