using ExtractConstantsAnalyzer.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExtractConstantsAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractConstantsAnalyzerCodeFixProvider)), Shared]
    public class ExtractConstantsAnalyzerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(ExtractConstantsAnalyzerAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var les = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().First();
            var lesKind = les.Kind();

            if (!ExtractConstantsAnalyzerAnalyzer.IsSubject(lesKind))
            {
                return;
            }

            var lesMember = les.GoUpTo<MemberDeclarationSyntax>();

            var cds = les.GoUpTo<ClassDeclarationSyntax>();
            var cds2Members = cds.Members;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var lesValue = semanticModel.GetConstantValue(les).Value;
            var classSymbol = semanticModel.GetDeclaredSymbol(cds);

            var classSymbolMembers = classSymbol.GetMembers();

            var isNeedToExtractConstant = IsNeedToExtractConstant(classSymbolMembers, les, lesValue, out var constantName);
            if (constantName is null)
            {
                constantName = classSymbol.Name + FilterString(les.Token.Text);
                if(IsConstantAlreadyExists(classSymbolMembers, constantName))
                {
                    //such constant already exists but with different value
                    //we need to randomize constant's name
                    constantName += Guid.NewGuid().ToString().Replace("-", "");
                }
            }

            //replace in-place literal with constant name
            var lesToReplace = SyntaxFactory.IdentifierName(constantName);
            var newLesMember = lesMember.ReplaceNode(les, lesToReplace);
            cds2Members = cds2Members.Replace(lesMember, newLesMember);

            //add new constant into the class
            if (isNeedToExtractConstant)
            {
                var firstCdsMember = cds2Members.FirstOrDefault();
                var mf = firstCdsMember.GetLocation().SourceTree.ToString();
                var mt = mf.TrimStart();
                var prefix = mf.Substring(0, mf.Length - mt.Length);

                SyntaxKind constantType = GetSyntaxKindForConstantType(les.Token);
                SyntaxKind constantValueType = GetSyntaxKindForConstantValueType(constantType);

                var literal = GetSyntaxTokenForConstantLiteal(constantType, lesValue);

                var newContantDeclaration = SyntaxFactory.FieldDeclaration(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.PredefinedType(
                            SyntaxFactory.Token(constantType)
                            )
                        )
                        .WithVariables(
                            SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(constantName))
                                .WithInitializer(
                                    SyntaxFactory.EqualsValueClause(
                                        SyntaxFactory.LiteralExpression(
                                            constantValueType,
                                            literal
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            new[]
                            {
                            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                            SyntaxFactory.Token(SyntaxKind.ConstKeyword)}
                            )
                        )
                    .NormalizeWhitespace()
                    .WithLeadingTrivia(SyntaxFactory.ParseTrailingTrivia(prefix))
                    .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(Environment.NewLine))
                    ;

                cds2Members = cds2Members.Insert(
                    0,
                    newContantDeclaration
                    );
            }



            var cds2 = cds.WithMembers(cds2Members);
            var root2 = root.ReplaceNode(cds, cds2);
            document = document.WithSyntaxRoot(root2);

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: CodeFixResources.CodeFixTitle,
                    createChangedDocument: ct => Task.FromResult(document),
                    equivalenceKey: nameof(CodeFixResources.CodeFixTitle)),
                diagnostic);
        }

        private string FilterString(string text)
        {
            text = text.Trim('"', '\'');
            var sb = new StringBuilder(text.Length);

            foreach(var c in text)
            {
                if(char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            return sb.ToString();
        }

        private SyntaxToken GetSyntaxTokenForConstantLiteal(SyntaxKind constantType, object lesValue)
        {
            switch (constantType)
            {
                case SyntaxKind.StringKeyword:
                    return SyntaxFactory.Literal((string)lesValue);
                case SyntaxKind.CharKeyword:
                    return SyntaxFactory.Literal((char)lesValue);
                case SyntaxKind.ULongKeyword:
                    return SyntaxFactory.Literal((ulong)lesValue);
                case SyntaxKind.LongKeyword:
                    return SyntaxFactory.Literal((long)lesValue);
                case SyntaxKind.UIntKeyword:
                    return SyntaxFactory.Literal((uint)lesValue);
                case SyntaxKind.DecimalKeyword:
                    return SyntaxFactory.Literal((decimal)lesValue);
                case SyntaxKind.DoubleKeyword:
                    return SyntaxFactory.Literal((double)lesValue);
                case SyntaxKind.FloatKeyword:
                    return SyntaxFactory.Literal((float)lesValue);
                case SyntaxKind.IntKeyword:
                    return SyntaxFactory.Literal((int)lesValue);
            }


            throw new InvalidOperationException($"Unknown constant type: {constantType}, value: {lesValue}");
        }

        private SyntaxKind GetSyntaxKindForConstantValueType(SyntaxKind constantType)
        {
            switch (constantType)
            {
                case SyntaxKind.StringKeyword:
                    return SyntaxKind.StringLiteralExpression;
                case SyntaxKind.CharKeyword:
                    return SyntaxKind.CharacterLiteralExpression;
                default:
                    return SyntaxKind.NumericLiteralExpression;
            }
        }

        private SyntaxKind GetSyntaxKindForConstantType(SyntaxToken token)
        {
            if(token.IsKind(SyntaxKind.StringLiteralToken))
            {
                return SyntaxKind.StringKeyword;
            }
            if (token.IsKind(SyntaxKind.CharacterLiteralToken))
            {
                return SyntaxKind.CharKeyword;
            }

            var text = token.Text.ToLower();

            if(text.EndsWith("ul"))
            {
                return SyntaxKind.ULongKeyword;
            }
            if (text.EndsWith("l"))
            {
                return SyntaxKind.LongKeyword;
            }

            if (text.EndsWith("u"))
            {
                return SyntaxKind.UIntKeyword;
            }

            if (text.EndsWith("m"))
            {
                return SyntaxKind.DecimalKeyword;
            }

            if (text.EndsWith("d"))
            {
                return SyntaxKind.DoubleKeyword;
            }

            if (text.EndsWith("f"))
            {
                return SyntaxKind.FloatKeyword;
            }

            return SyntaxKind.IntKeyword;
        }

        private static bool IsConstantAlreadyExists(
            ImmutableArray<ISymbol> members,
            string constantName
            )
        {
            foreach (var member in members)
            {
                if (member is not IFieldSymbol field)
                {
                    continue;
                }
                if (!field.IsConst)
                {
                    continue;
                }

                if(field.Name == constantName)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNeedToExtractConstant(
            ImmutableArray<ISymbol> members,
            LiteralExpressionSyntax les,
            object lesValue,
            out string? constantName
            )
        {
            foreach (var member in members)
            {
                if (member is not IFieldSymbol field)
                {
                    continue;
                }
                if (!field.IsConst)
                {
                    continue;
                }

                if (lesValue != null && field.ConstantValue != null)
                {
                    if (lesValue.GetType() == field.ConstantValue.GetType())
                    {
                        if (ExtractConstantsAnalyzerAnalyzer.IsNumeric(les.Kind()))
                        {
                            if ((int)lesValue == (int)field.ConstantValue)
                            {
                                //such constant already exists!
                                constantName = field.Name;
                                return false;
                            }
                        }
                        else
                        {
                            if (lesValue == field.ConstantValue)
                            {
                                //such constant already exists!
                                constantName = field.Name;
                                return false;
                            }
                        }
                    }
                }
            }

            constantName = null;
            return true;
        }

    }
}
