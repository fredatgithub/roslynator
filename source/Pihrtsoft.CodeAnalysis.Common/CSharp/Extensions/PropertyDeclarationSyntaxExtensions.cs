﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Pihrtsoft.CodeAnalysis.CSharp
{
    public static class PropertyDeclarationExtensions
    {
        public static bool IsReadOnlyProperty(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            if (propertyDeclaration.AccessorList == null)
                return false;

            if (propertyDeclaration.AccessorList.Accessors.Count != 1)
                return false;

            AccessorDeclarationSyntax accessor = propertyDeclaration.AccessorList.Accessors[0];

            if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                return false;

            if (accessor.Body == null)
                return false;

            return true;
        }

        public static bool IsReadOnlyAutoProperty(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            if (propertyDeclaration.AccessorList == null)
                return false;

            if (propertyDeclaration.AccessorList.Accessors.Count != 1)
                return false;

            AccessorDeclarationSyntax accessor = propertyDeclaration.AccessorList.Accessors[0];

            if (!accessor.IsKind(SyntaxKind.GetKeyword))
                return false;

            if (accessor.Body != null)
                return false;

            return true;
        }

        public static bool IsAutoProperty(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            if (propertyDeclaration.AccessorList == null)
                return false;

            return propertyDeclaration
                .AccessorList
                .Accessors.All(f => f.Body == null);
        }

        public static AccessorDeclarationSyntax Getter(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            if (propertyDeclaration.AccessorList == null)
                return null;

            return propertyDeclaration.AccessorList.Getter();
        }

        public static AccessorDeclarationSyntax Setter(this PropertyDeclarationSyntax propertyDeclaration)
        {
            if (propertyDeclaration == null)
                throw new ArgumentNullException(nameof(propertyDeclaration));

            if (propertyDeclaration.AccessorList == null)
                return null;

            return propertyDeclaration.AccessorList.Setter();
        }

        public static bool ContainsGetter(this PropertyDeclarationSyntax propertyDeclaration)
            => Getter(propertyDeclaration) != null;

        public static bool ContainsSetter(this PropertyDeclarationSyntax propertyDeclaration)
            => Setter(propertyDeclaration) != null;
    }
}
