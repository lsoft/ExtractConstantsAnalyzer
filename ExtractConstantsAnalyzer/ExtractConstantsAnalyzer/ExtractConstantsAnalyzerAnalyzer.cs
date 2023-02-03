using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ExtractConstantsAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExtractConstantsAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ExtractConstantsAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Naming";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static async void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            try
            {
                var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

                foreach (var dsr in namedTypeSymbol.DeclaringSyntaxReferences)
                {
                    var syntaxTree = await dsr.GetSyntaxAsync(context.CancellationToken);

                    var methods = syntaxTree
                        .DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .ToList();

                    foreach(var methodSyntax in methods)
                    {
                        var literals = methodSyntax
                            .DescendantNodes()
                            .OfType<LiteralExpressionSyntax>()
                            .ToList();

                        foreach(var literalSyntax in literals)
                        {
                            var kind = literalSyntax.Kind();
                            if(IsSubject(kind))
                            {
                                if (!string.IsNullOrEmpty(literalSyntax.Token.ValueText))
                                {
                                    var diagnostic = Diagnostic.Create(Rule, literalSyntax.GetLocation(), literalSyntax.Token.ValueText);
                                    context.ReportDiagnostic(diagnostic);
                                }
                            }
                        }
                    }
                }
            }
            catch(Exception excp)
            {
                Debug.WriteLine(excp.Message + Environment.NewLine + excp.StackTrace);
            }
        }

        public static bool IsSubject(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.CharacterLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                case SyntaxKind.NumericLiteralExpression:
                    return true;
            }

            return false;
        }

        public static bool IsNumeric(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.CharacterLiteralExpression:
                case SyntaxKind.StringLiteralExpression:
                    return false;
                case SyntaxKind.NumericLiteralExpression:
                    return true;
                default:
                    throw new InvalidOperationException(kind.ToString());
            }
        }


    }
}
