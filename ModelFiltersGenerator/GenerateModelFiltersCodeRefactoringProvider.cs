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
using Microsoft.VisualStudio.Shell;
using ModelFiltersGenerator.Analyzers;
using ModelFiltersGenerator.Generators;
using ModelFiltersGenerator.Models;
using Task = System.Threading.Tasks.Task;

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

            var action = CustomCodeAction.Create(
                "Generate filters for model",
                (ct, previewMode) => GenerateModelFiltersAsync(document.Project.Solution, document.Project.Id, root.GetNamespaceName(), token.Text, properties, previewMode, ct),
                equivalenceKey: nameof(GenerateModelFiltersCodeRefactoringProvider));

            context.RegisterRefactoring(action);
        }

        private async Task<Solution> GenerateModelFiltersAsync(
            Solution solution,
            ProjectId projectId,
            string namespaceName,
            string className,
            IEnumerable<PropertyInfo> properties,
            bool previewMode,
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

            if (!previewMode)
            {
                ThreadHelper.Generic.BeginInvoke(() =>
                {
                    solution.Workspace.OpenDocument(documentId);
                });
            }

            return solution.WithDocumentSyntaxRoot(documentId, formattedRoot);
        }
    }

    internal class CustomCodeAction : CodeAction
    {
        private readonly Func<CancellationToken, bool, Task<Solution>> createChangedSolution;

        public override string EquivalenceKey { get; }
        public override string Title { get; }

        protected CustomCodeAction(string title, Func<CancellationToken, bool, Task<Solution>> createChangedSolution, string equivalenceKey = null)
        {
            this.createChangedSolution = createChangedSolution;

            Title = title;
            EquivalenceKey = equivalenceKey;
        }

        /// <summary>
        ///     Creates a <see cref="CustomCodeAction" /> for a change to more than one <see cref="Document" /> within a <see cref="Solution" />.
        ///     Use this factory when the change is expensive to compute and should be deferred until requested.
        /// </summary>
        /// <param name="title">Title of the <see cref="CustomCodeAction" />.</param>
        /// <param name="createChangedSolution">Function to create the <see cref="Solution" />.</param>
        /// <param name="equivalenceKey">Optional value used to determine the equivalence of the <see cref="CustomCodeAction" /> with other <see cref="CustomCodeAction" />s. See <see cref="CustomCodeAction.EquivalenceKey" />.</param>
        public static CustomCodeAction Create(string title, Func<CancellationToken, bool, Task<Solution>> createChangedSolution, string equivalenceKey = null)
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            if (createChangedSolution == null)
                throw new ArgumentNullException(nameof(createChangedSolution));

            return new CustomCodeAction(title, createChangedSolution, equivalenceKey);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
        {
            const bool isPreview = true;
            var changedSolution = await GetChangedSolutionWithPreviewAsync(cancellationToken, isPreview).ConfigureAwait(false);

            if (changedSolution == null) return null;

            return new CodeActionOperation[] { new ApplyChangesOperation(changedSolution) };
        }

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            const bool isPreview = false;
            return GetChangedSolutionWithPreviewAsync(cancellationToken, isPreview);
        }

        protected virtual Task<Solution> GetChangedSolutionWithPreviewAsync(CancellationToken cancellationToken, bool isPreview)
        {
            return createChangedSolution(cancellationToken, isPreview);
        }
    }
}
