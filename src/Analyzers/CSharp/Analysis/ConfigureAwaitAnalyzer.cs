﻿// Copyright (c) .NET Foundation and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslynator.CSharp.CodeStyle;
using Roslynator.CSharp.Syntax;

namespace Roslynator.CSharp.Analysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConfigureAwaitAnalyzer : BaseDiagnosticAnalyzer
{
    private static ImmutableArray<DiagnosticDescriptor> _supportedDiagnostics;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
    {
        get
        {
            if (_supportedDiagnostics.IsDefault)
            {
                Immutable.InterlockedInitialize(ref _supportedDiagnostics, DiagnosticRules.ConfigureAwait);
            }

            return _supportedDiagnostics;
        }
    }

    public override void Initialize(AnalysisContext context)
    {
        base.Initialize(context);

        context.RegisterSyntaxNodeAction(
            c =>
            {
                ConfigureAwaitStyle style = c.GetConfigureAwaitStyle();

                if (style == ConfigureAwaitStyle.Omit)
                {
                    RemoveCallToConfigureAwait(c);
                }
                else if (style == ConfigureAwaitStyle.Include
                    && c.Compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ConfiguredTaskAwaitable`1") is not null)
                {
                    AddCallToConfigureAwait(c);
                }
            },
            SyntaxKind.AwaitExpression);
    }

    private static void AddCallToConfigureAwait(SyntaxNodeAnalysisContext context)
    {
        var awaitExpression = (AwaitExpressionSyntax)context.Node;

        ExpressionSyntax expression = awaitExpression.Expression;

        if (IsConfigureAwait(expression))
            return;

        ITypeSymbol typeSymbol = context.SemanticModel.GetTypeSymbol(expression, context.CancellationToken);

        if (typeSymbol is null)
            return;

        if (!typeSymbol.IsAwaitable(context.SemanticModel, expression.SpanStart))
            return;

        if (!IsConfigureAwaitable(typeSymbol, context.SemanticModel, expression.SpanStart))
            return;

        DiagnosticHelpers.ReportDiagnostic(context, DiagnosticRules.ConfigureAwait, awaitExpression.Expression, "Add");
    }

    private static void RemoveCallToConfigureAwait(SyntaxNodeAnalysisContext context)
    {
        var awaitExpression = (AwaitExpressionSyntax)context.Node;

        // await (expr).ConfigureAwait(false);
        //       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        ExpressionSyntax expression = awaitExpression.Expression;

        // await (expr).ConfigureAwait(false);
        //             ^^^^^^^^^^^^^^^^^^^^^^
        SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(expression);

        if (!IsConfigureAwaitFalse(invocationInfo, context.SemanticModel, context.CancellationToken))
            return;

        ITypeSymbol awaitedType = context.SemanticModel.GetTypeSymbol(expression, context.CancellationToken);

        if (awaitedType is null)
            return;

        if (!awaitedType.IsAwaitable(context.SemanticModel, expression.SpanStart))
            return;

        // await (expr).ConfigureAwait(false);
        //        ^^^^
        // This expression may not be awaitable, in which case removing ConfigureAwait is not possible.
        ITypeSymbol configuredType = context.SemanticModel.GetTypeSymbol(invocationInfo.Expression, context.CancellationToken);

        if (configuredType is null)
            return;

        if (!configuredType.IsAwaitable(context.SemanticModel, invocationInfo.Expression.SpanStart))
            return;

        DiagnosticHelpers.ReportDiagnostic(
            context,
            DiagnosticRules.ConfigureAwait,
            Location.Create(
                awaitExpression.SyntaxTree,
                TextSpan.FromBounds(invocationInfo.OperatorToken.SpanStart, expression.Span.End)),
            "Remove");
    }

    public static bool IsConfigureAwait(ExpressionSyntax expression)
    {
        SimpleMemberInvocationExpressionInfo invocationInfo = SyntaxInfo.SimpleMemberInvocationExpressionInfo(expression);

        return IsConfigureAwait(invocationInfo);
    }

    private static bool IsConfigureAwait(SimpleMemberInvocationExpressionInfo invocationInfo)
    {
        return invocationInfo.Success
            && invocationInfo.Name.IsKind(SyntaxKind.IdentifierName)
            && string.Equals(invocationInfo.NameText, "ConfigureAwait")
            && invocationInfo.Arguments.Count == 1;
    }

    private static bool IsConfigureAwaitFalse(SimpleMemberInvocationExpressionInfo invocationInfo, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        return IsConfigureAwait(invocationInfo)
            && semanticModel.GetConstantValue(invocationInfo.Arguments[0].Expression, cancellationToken).Value is bool boolValue
            && !boolValue;
    }

    private static bool IsConfigureAwaitable(ITypeSymbol typeSymbol, SemanticModel semanticModel, int position)
    {
        return semanticModel.LookupSymbols(position, typeSymbol, "ConfigureAwait", includeReducedExtensionMethods: true)
            .OfType<IMethodSymbol>()
            .Any(method => method.ReturnType.IsAwaitable(semanticModel, position)
                && method.HasSingleParameter(SpecialType.System_Boolean));
    }
}
