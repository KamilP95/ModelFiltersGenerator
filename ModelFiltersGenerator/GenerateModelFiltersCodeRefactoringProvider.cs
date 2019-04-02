using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using ModelFiltersGenerator.Analyzers;
using ModelFiltersGenerator.Generators;
using ModelFiltersGenerator.Models;

namespace ModelFiltersGenerator
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "GenerateModelFiltersCS")]
    internal class GenerateModelFiltersCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancelationToken = context.CancellationToken;

            var root = await document
                .GetSyntaxRootAsync(cancelationToken)
                .ConfigureAwait(false) as CompilationUnitSyntax;
            var semanticModel = await document.GetSemanticModelAsync(cancelationToken).ConfigureAwait(false);

            if (root == null || semanticModel == null)
            {
                return;
            }

            var token = root.FindToken(textSpan.Start);

            if (!token.IsClassNameToken())
            {
                return;
            }

            var properties = CodeAnalyzer.GetPropertiesInfo(token.Parent, semanticModel).ToArray();

            if (!properties.Any())
            {
                return;
            }

            var action = CodeAction.Create(
                "Generate filters for model",
                ct => GenerateModelFiltersAsync(document.Project.Solution, document.Project.Id, root.GetNamespaceName(), token.Text, properties, ct),
                equivalenceKey: nameof(GenerateModelFiltersCodeRefactoringProvider));

            context.RegisterRefactoring(action);
        }

        private async Task<Solution> GenerateModelFiltersAsync(
            Solution solution,
            ProjectId projectId,
            string namespaceName,
            string className,
            IEnumerable<PropertyInfo> properties,
            CancellationToken cancellationToken)
        {
            var filterModelClass = FilterModelGenerator.FilterModelClass(className, properties);
            var filterExtensionsClass = FilterExtensionsGenerator.FilterExtensionsClass(className, properties);
            var filtersRoot = BaseSyntaxGenerator.CompilationUnit(namespaceName, filterModelClass, filterExtensionsClass) as SyntaxNode;
            var documentId = DocumentId.CreateNewId(projectId);

            solution = solution.AddDocument(documentId, className + "Filters", filtersRoot);

            var newDoc = solution.GetDocument(documentId);

            var options = solution.Workspace.Options
                .WithChangedOption(FormattingOptions.IndentationSize, LanguageNames.CSharp, 3)
                .WithChangedOption(FormattingOptions.UseTabs, LanguageNames.CSharp, true)
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, Environment.NewLine)
                .WithChangedOption(FormattingOptions.SmartIndent, LanguageNames.CSharp, FormattingOptions.IndentStyle.Smart);

            newDoc = await Simplifier.ReduceAsync(newDoc, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);
            newDoc = await Formatter.FormatAsync(newDoc, Formatter.Annotation, options, cancellationToken).ConfigureAwait(false);

            var formattedRoot = await newDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            return solution.WithDocumentSyntaxRoot(documentId, formattedRoot);
        }
    }
}
