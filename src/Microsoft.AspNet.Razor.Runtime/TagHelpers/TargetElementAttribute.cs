// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Used to override a <see cref="ITagHelper"/>'s default element target.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class TargetElementAttribute : Attribute
    {
        /// <summary>
        /// Instantiates a new instance of the <see cref="TargetElementAttribute"/> class.
        /// </summary>
        public TargetElementAttribute()
        {
            Tags = TagHelperDescriptorProvider.CatchAllDescriptorTarget;
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TargetElementAttribute"/> class.
        /// </summary>
        /// <param name="tags">
        /// A comma separated <see cref="string"/> of tag names for the <see cref="ITagHelper"/> to target.
        /// </param>
        public TargetElementAttribute(string tags)
        {
            Tags = tags;
        }

        /// <summary>
        /// A comma separated <see cref="string"/> of tag names for the <see cref="ITagHelper"/> to target.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// A comma separated <see cref="string"/> of attribute names for the <see cref="ITagHelper"/> to target.
        /// </summary>
        public string Attributes { get; set; }
    }
}