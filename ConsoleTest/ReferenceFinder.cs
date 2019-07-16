using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp3
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

        public Stack<MemberDeclarationSyntax>[] Results
        {
            get {
                if (results == null)
                {
                    results = new HashSet<Stack<MemberDeclarationSyntax>>();
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
        private HashSet<Stack<MemberDeclarationSyntax>> results;

        private IEnumerable<Stack<MemberDeclarationSyntax>> FindAllReferencesForDocument()
        {
            var symbols = GetSymbols().ToArray();

            foreach (var symbol in symbols)
                foreach (var reference in FindReferencesForSymbol(symbol, new Stack<MemberDeclarationSyntax>()))
                    if (!results.Contains(reference))
                        results.Add(reference);

            return results;
        }

        private IEnumerable<ISymbol> GetSymbols()
        {
            if (codeLines.Length == 0)
                return solution.GetClassDeclarationsSymbols(startingDocument);
            else
            {
                var root = startingDocument.GetSyntaxRootAsync().Result.DescendantNodes();
                var declarations = root.OfType<MethodDeclarationSyntax>().Cast<MemberDeclarationSyntax>()
                    .Union(root.OfType<ConstructorDeclarationSyntax>());

                return codeLines.Select(line => 
                    declarations.Where(n =>
                    {
                        var lineSpan = n.GetLocation().GetLineSpan();
                        return lineSpan.StartLinePosition.Line < line && lineSpan.EndLinePosition.Line > line;
                    }).Select(x => solution.GetMethodDeclarationSymbol(startingDocument, x)).First());
            }
        }

        private IEnumerable<Stack<MemberDeclarationSyntax>> FindReferencesForSymbol(ISymbol symbol, Stack<MemberDeclarationSyntax> previousStack)
        {
            if (searchedSymbols.Contains(symbol))
                yield break;

            searchedSymbols.Add(symbol);

            var referencesToM = SymbolFinder.FindReferencesAsync(symbol, startingDocument.Project.Solution).Result;

            foreach (var reference in referencesToM)
            {
                foreach (var locationRef in reference.Locations)
                {
                    var currentStack = new Stack<MemberDeclarationSyntax>(new Stack<MemberDeclarationSyntax>(previousStack));

                    var location = locationRef.Location;
                    var methodDeclaration = FindMethodDeclaration(location);
                    currentStack.Push(methodDeclaration);

                    if (IsValidNamespace(location))
                        yield return currentStack;
                    else
                    {
                        var methodDeclarationSymbol = solution.GetMethodDeclarationSymbol(locationRef.Document, methodDeclaration);

                        
                        foreach (var recursiveReference in FindReferencesForSymbol(methodDeclarationSymbol, currentStack))
                            yield return recursiveReference;
                    }
                }
            }
        }

        private MemberDeclarationSyntax FindMethodDeclaration(Location location)
        {
            var ancestors = GetAncestors(location);

            // TODO: Maybe implement this using polymorphism or a visitor Pattern.
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
