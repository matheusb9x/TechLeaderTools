using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp3
{
    public class SolutionExplorer
    {
        public SolutionExplorer()
        {
            this.semanticModelMaps = new Dictionary<DocumentId, SemanticModel>();
        }

        public SolutionExplorer(string solutionPath) : this()
        {
            workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += Workspace_WorkspaceFailed;

            solution = workspace.OpenSolutionAsync(solutionPath).Result;
        }

        public Document GetDocument(string fileNameWithExtension)
        {
            var results = solution.Projects
                .SelectMany(p => p.Documents)
                .Where(d => d.Name == fileNameWithExtension);

            if (results.Count() > 1)
                throw new Exception("More than one file found.");

            Document result = results.First();

            return result;
        }

        public IEnumerable<ISymbol> GetClassDeclarationsSymbols(Document document)
        {
            //var classDeclarations = document
            //        .GetSyntaxRootAsync().Result
            //        .DescendantNodes()
            //        .OfType<ClassDeclarationSyntax>();

            //foreach (var classDeclaration in classDeclarations)
            //{
            //    yield return GetMethodDeclarationSymbol(document, classDeclaration);
            //}

            var firstMethod = document.GetSyntaxRootAsync().Result.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            Console.WriteLine("First method name:" + firstMethod.Identifier.ToString() + firstMethod.ParameterList.ToString());

            yield return GetMethodDeclarationSymbol(document, firstMethod);
        }

        public ISymbol GetMethodDeclarationSymbol(Document document, MemberDeclarationSyntax declarationNode)
        {
            var model = GetSemanticModel(document);
            return model.GetSymbolInfo(declarationNode).Symbol ?? model.GetDeclaredSymbol(declarationNode);
        }

        public ReferenceFinder FindReferences(Document document, string[] namespaces, int[] codeLines)
        {
            return new ReferenceFinder(this, document, namespaces, codeLines);
        }

        public SemanticModel GetSemanticModel(Document document)
        {
            if (!semanticModelMaps.ContainsKey(document.Id))
                semanticModelMaps.Add(document.Id, document.GetSemanticModelAsync().Result);

            return semanticModelMaps[document.Id];
        }

        private Solution solution;
        private MSBuildWorkspace workspace;
        private Dictionary<DocumentId, SemanticModel> semanticModelMaps;

        private static void Workspace_WorkspaceFailed(object sender, Microsoft.CodeAnalysis.WorkspaceDiagnosticEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
