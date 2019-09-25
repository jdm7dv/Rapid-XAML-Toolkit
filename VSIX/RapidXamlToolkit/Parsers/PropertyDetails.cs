﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace RapidXamlToolkit.Parsers
{
    public class PropertyDetails
    {
        public static PropertyDetails Empty => new PropertyDetails()
        {
            Name = string.Empty,
            PropertyType = string.Empty,
            IsReadOnly = false,
            Symbol = null,
        };

        public string Name { get; set; }

        public string PropertyType { get; set; }

        public bool IsReadOnly { get; set; }

        public ITypeSymbol Symbol { get; set; }

        public List<AttributeDetails> Attributes { get; set; } = new List<AttributeDetails>();
    }
}
