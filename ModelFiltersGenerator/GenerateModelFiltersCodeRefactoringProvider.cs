using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

            if (!CodeAnalyzer.IsClassNameToken(token))
            {
                return;
            }

            var properties = CodeAnalyzer.GetPropertiesInfo(token.Parent, semanticModel).ToArray();

            if (!properties.Any())
            {
                return;
            }

            var action = CodeAction.Create(
                "Create filters for model",
                ct => GenerateModelFiltersAsync(document, root, token, properties, ct),
                equivalenceKey: nameof(GenerateModelFiltersCodeRefactoringProvider));

            context.RegisterRefactoring(action);
        }

        private Task<Solution> GenerateModelFiltersAsync(
            Document document,
            CompilationUnitSyntax root,
            SyntaxToken classNameToken,
            IEnumerable<PropertyInfo> properties,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var className = classNameToken.Text + "Filters";
            var filterClass = CodeGenerator.CreateFilterClass(className, properties);
            var filtersRoot = CodeGenerator.CreateRoot(root.GetNamespaceName(), filterClass);
            var documentId = DocumentId.CreateNewId(document.Project.Id);

            solution = solution.AddDocument(documentId, className, filtersRoot);

            return Task.FromResult(solution);
        }
    }
}
