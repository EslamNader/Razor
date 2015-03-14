// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Runtime.TagHelpers
{
    /// <summary>
    /// Provides an <see cref="ITagHelper"/>'s target.
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
        /// A comma-separated <see cref="string"/> of tag names the <see cref="ITagHelper"/> targets.
        /// </param>
        /// <remarks><paramref name="tags"/> being set to <c>*</c> results in an <see cref="ITagHelper"/> 
        /// targeting all HTML elements that have the provided <see cref="Attributes"/>.</remarks>
        public TargetElementAttribute(string tags)
            : this(tags, attributes: null)
        {
        }

        /// <summary>
        /// Instantiates a new instance of the <see cref="TargetElementAttribute"/> class.
        /// </summary>
        /// <param name="tags">
        /// A comma-separated <see cref="string"/> of tag names the <see cref="ITagHelper"/> targets.
        /// </param>
        /// <param name="attributes">
        /// A comma-separated <see cref="string"/> of attribute names the <see cref="ITagHelper"/> targets.
        /// </param>
        /// <remarks><paramref name="tags"/> being set to <c>*</c> results in an <see cref="ITagHelper"/> 
        /// targeting all HTML elements that have the provided <see cref="Attributes"/>.</remarks>
        public TargetElementAttribute(string tags, string attributes)
        {
            Tags = tags;
            Attributes = attributes;
        }

        /// <summary>
        /// A comma-separated <see cref="string"/> of tag names the <see cref="ITagHelper"/> targets.
        /// </summary>
        /// <remarks><see cref="Tags"/> being set to <c>*</c> results in an <see cref="ITagHelper"/> 
        /// targeting all HTML elements that have the provided <see cref="Attributes"/>.</remarks>
        public string Tags { get; }

        /// <summary>
        /// A comma-separated <see cref="string"/> of attribute names the <see cref="ITagHelper"/> targets.
        /// </summary>
        public string Attributes { get; set; }
    }
}