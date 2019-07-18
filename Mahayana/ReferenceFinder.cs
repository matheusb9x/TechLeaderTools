using Mahayana;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mayahana
{
    public class ReferenceFinder
    {
        private ReferenceFinder()
        {
            this.searchedSymbols = new HashSet<ISymbol>();
        }

        internal ReferenceFinder(SolutionExplorer solution, Document document, string[] namespaces, int[] codeLines) : this()
        {
            this.solution = solution;
            this.namespaces = namespaces;
            this.startingDocument = document;
            this.codeLines = codeLines;
        }

        public ReferenceFinderResult.Item[] Results
        {
            get {
                if (results == null)
                {
                    results = new ReferenceFinderResult();
                    FindAllReferencesForDocument();
                }

                return results.ToArray();
            }
        }

        private SolutionExplorer solution;
        private string[] namespaces;
        private int[] codeLines;
        private Document startingDocument;
        private HashSet<ISymbol> searchedSymbols;
        private ReferenceFinderResult results;

        private IEnumerable<ReferenceFinderResult.Item> FindAllReferencesForDocument()
        {
            var symbols = GetLocationSymbols().ToArray();

            foreach (var symbol in symbols)
                foreach (var reference in FindReferencesForSymbol(symbol, new ReferenceFinderResult.Item()))
                    if (!results.Contains(reference))
                        results.Add(reference);

            return results;
        }

        private IEnumerable<ISymbol> GetLocationSymbols()
        {
            if (codeLines.Length == 0)
                throw new ArgumentException("No lines were specified.");
            else
            {
                var root = startingDocument.GetSyntaxRootAsync().Result.DescendantNodes();
                var declarations = root.OfType<MethodDeclarationSyntax>().Cast<MemberDeclarationSyntax>()
                    .Union(root.OfType<ConstructorDeclarationSyntax>());

                return codeLines.Select(line =>
                    declarations
                        .Where(n => {
                            var lineSpan = n.GetLocation().GetLineSpan();
                            return lineSpan.StartLinePosition.Line < line && lineSpan.EndLinePosition.Line > line;
                        })
                        .Select(x => solution.GetMemberDeclarationSymbol(startingDocument, x))
                        .First()
                );
            }
        }

        private IEnumerable<ReferenceFinderResult.Item> FindReferencesForSymbol(ISymbol symbol, ReferenceFinderResult.Item previousStack)
        {
            if (searchedSymbols.Contains(symbol))
                yield break;

            searchedSymbols.Add(symbol);

            var referencesToM = SymbolFinder.FindReferencesAsync(symbol, startingDocument.Project.Solution).Result;

            foreach (var reference in referencesToM)
            {
                foreach (var locationRef in reference.Locations)
                {
                    var currentStack = previousStack.Clone();

                    var location = locationRef.Location;
                    var memberDeclaration = FindMemberDeclaration(location);
                    currentStack.Add(memberDeclaration);

                    if (IsValidNamespace(location))
                        yield return currentStack;
                    else
                    {
                        var methodDeclarationSymbol = solution.GetMemberDeclarationSymbol(locationRef.Document, memberDeclaration);
                        
                        foreach (var recursiveReference in FindReferencesForSymbol(methodDeclarationSymbol, currentStack))
                            yield return recursiveReference;
                    }
                }
            }
        }

        private MemberDeclarationSyntax FindMemberDeclaration(Location location)
        {
            var ancestors = GetAncestors(location);

            var methodDeclaration = ancestors.OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration != null)
                return methodDeclaration;

            var constructorDeclaration = ancestors.OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
            if (constructorDeclaration != null)
                return constructorDeclaration;

            var propertyDeclaration = ancestors.OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyDeclaration != null)
                return propertyDeclaration;

            // In case its used in the generics, or something wrong happened...
            var classDeclaration = ancestors.OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration != null)
                return classDeclaration;

            throw new Exception("I dont know where this method call is...");
        }

        private bool IsValidNamespace(Location location)
        {
            var ancestors = GetAncestors(location);
            var namespaceDeclaration = ancestors.OfType<NamespaceDeclarationSyntax>().First();
            string locationNamespace = namespaceDeclaration.Name.ToString();

            foreach (var ns in namespaces)
            {
                if (locationNamespace.StartsWith(ns))
                    return true;
            }

            return false;
        }

        private IEnumerable<SyntaxNode> GetAncestors(Location location)
        {
            return location.SourceTree.GetRoot().FindNode(location.SourceSpan).Ancestors();
        }
    }
}
