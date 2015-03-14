﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    public class CompleteTagHelperDescriptorComparer : TagHelperDescriptorComparer, IEqualityComparer<TagHelperDescriptor>
    {
        public new static readonly CompleteTagHelperDescriptorComparer Default =
            new CompleteTagHelperDescriptorComparer();

        private CompleteTagHelperDescriptorComparer()
        {
        }

        bool IEqualityComparer<TagHelperDescriptor>.Equals(TagHelperDescriptor descriptorX, TagHelperDescriptor descriptorY)
        {
            return base.Equals(descriptorX, descriptorY) &&
                   // Tests should be exact casing
                   string.Equals(descriptorX.TagName, descriptorY.TagName, StringComparison.Ordinal) &&
                   string.Equals(descriptorX.Prefix, descriptorY.Prefix, StringComparison.Ordinal) &&
                   Enumerable.SequenceEqual(
                       descriptorX.RequiredAttributes,
                       descriptorY.RequiredAttributes,
                       StringComparer.OrdinalIgnoreCase) &&
                   descriptorX.Attributes.SequenceEqual(
                       descriptorY.Attributes,
                        CompleteTagHelperAttributeDescriptorComparer.Default);
        }

        int IEqualityComparer<TagHelperDescriptor>.GetHashCode(TagHelperDescriptor descriptor)
        {
            return HashCodeCombiner
                .Start()
                .Add(base.GetHashCode())
                .Add(descriptor.TagName, StringComparer.Ordinal)
                .Add(descriptor.Prefix)
                .Add(descriptor.Attributes)
                .CombinedHash;
        }

        private class CompleteTagHelperAttributeDescriptorComparer : IEqualityComparer<TagHelperAttributeDescriptor>
        {
            public static readonly CompleteTagHelperAttributeDescriptorComparer Default =
                new CompleteTagHelperAttributeDescriptorComparer();

            private CompleteTagHelperAttributeDescriptorComparer()
            {
            }

            public bool Equals(TagHelperAttributeDescriptor descriptorX, TagHelperAttributeDescriptor descriptorY)
            {
                return
                    // Tests should be exact casing
                    string.Equals(descriptorX.Name, descriptorY.Name, StringComparison.Ordinal) &&
                    string.Equals(descriptorX.PropertyName, descriptorY.PropertyName, StringComparison.Ordinal) &&
                    string.Equals(descriptorX.TypeName, descriptorY.TypeName, StringComparison.Ordinal);
            }

            public int GetHashCode(TagHelperAttributeDescriptor descriptor)
            {
                return HashCodeCombiner
                    .Start()
                    .Add(descriptor.Name, StringComparer.Ordinal)
                    .Add(descriptor.PropertyName, StringComparer.Ordinal)
                    .Add(descriptor.TypeName, StringComparer.Ordinal)
                    .CombinedHash;
            }
        }
    }
}