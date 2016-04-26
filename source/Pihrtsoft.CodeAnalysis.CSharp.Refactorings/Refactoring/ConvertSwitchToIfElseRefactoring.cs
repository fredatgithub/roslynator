﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Pihrtsoft.CodeAnalysis.CSharp.Refactoring
{
    internal static class ConvertSwitchToIfElseRefactoring
    {
        public static async Task<Document> RefactorAsync(
            Document document,
            SwitchStatementSyntax switchStatement,
            CancellationToken cancellationToken)
        {
            SyntaxNode oldRoot = await document.GetSyntaxRootAsync(cancellationToken);

            IfStatementSyntax newNode = ConvertSwitchToIfElse(switchStatement)
                .WithTriviaFrom(switchStatement)
                .WithAdditionalAnnotations(Formatter.Annotation);

            SyntaxNode newRoot = oldRoot.ReplaceNode(switchStatement, newNode);

            return document.WithSyntaxRoot(newRoot);
        }

        private static IfStatementSyntax ConvertSwitchToIfElse(SwitchStatementSyntax switchStatement)
        {
            IfStatementSyntax ifStatement = null;
            ElseClauseSyntax elseClause = null;

            int index = switchStatement.Sections.IndexOf(f => f.Labels.IndexOf(SyntaxKind.DefaultSwitchLabel) != -1);

            if (index != -1)
            {
                SyntaxList<StatementSyntax> statements = RemoveBreakStatement(switchStatement.Sections[index].Statements);

                elseClause = ElseClause(Block(statements));
            }

            for (int i = switchStatement.Sections.Count - 1; i >= 0; i--)
            {
                if (i == index)
                    continue;

                SyntaxList<StatementSyntax> statements = RemoveBreakStatement(switchStatement.Sections[i].Statements);

                IfStatementSyntax @if = IfStatement(
                    CreateCondition(switchStatement, switchStatement.Sections[i]),
                    Block(statements));

                if (ifStatement != null)
                {
                    ifStatement = @if.WithElse(ElseClause(ifStatement));
                }
                else
                {
                    ifStatement = @if;

                    if (elseClause != null)
                        ifStatement = ifStatement.WithElse(elseClause);
                }
            }

            return ifStatement;
        }

        private static ExpressionSyntax CreateCondition(SwitchStatementSyntax switchStatement, SwitchSectionSyntax switchSection)
        {
            ExpressionSyntax condition = null;

            for (int i = switchSection.Labels.Count - 1; i >= 0; i--)
            {
                BinaryExpressionSyntax equalsExpression = BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    switchStatement.Expression,
                    ((CaseSwitchLabelSyntax)switchSection.Labels[i]).Value);

                if (condition != null)
                {
                    condition = BinaryExpression(
                        SyntaxKind.LogicalOrExpression,
                        equalsExpression,
                        condition);
                }
                else
                {
                    condition = equalsExpression;
                }
            }

            return condition;
        }

        private static SyntaxList<StatementSyntax> RemoveBreakStatement(SyntaxList<StatementSyntax> statements)
        {
            int index = statements.IndexOf(SyntaxKind.BreakStatement);

            if (index != -1)
                return statements.RemoveAt(index);

            return statements;
        }
    }
}
