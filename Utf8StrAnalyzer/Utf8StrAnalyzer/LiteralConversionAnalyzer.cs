
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;


namespace Utf8StrAnalyzer;
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LiteralConversionAnalyzer: DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conversion);//,,OperationKind.Argument
    }
    void AnalyzeOperation(OperationAnalysisContext context)
    {
        var conversionOperation = (IConversionOperation)context.Operation;
        if (conversionOperation is not { IsImplicit: true, Conversion.IsUserDefined: true, Type.Name: "u8str" }) return;
        var operandSyntax = conversionOperation.Operand.Syntax;
        if (!operandSyntax.IsKind(SyntaxKind.Utf8StringLiteralExpression))
        {
            var diagnostic = Diagnostic.Create(DiagnosticDescriptors.U8StrAnalyzer, operandSyntax.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }=ImmutableArray.Create(DiagnosticDescriptors.U8StrAnalyzer);
}

public static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor U8StrAnalyzer = new("U8STR001", "U8StrAnalyzer", "Only utf8 string literals can be implicitly converted to u8str", "Usage", DiagnosticSeverity.Error, true);
}
