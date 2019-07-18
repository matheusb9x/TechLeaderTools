using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mahayana
{
    public class CodeNavigator
    {
        static CodeNavigator()
        {
            OfTypeDelegates = new Dictionary<Type, Func<IEnumerable<SyntaxNode>, IEnumerable<MemberDeclarationSyntax>>>();
            foreach (var type in SupportedDeclarationTypes)
            {
                var enumerableExtensions = typeof(System.Linq.Enumerable);
                var method = enumerableExtensions.GetMethod("OfType").MakeGenericMethod(type);

                var f = (Func<IEnumerable<SyntaxNode>, IEnumerable<MemberDeclarationSyntax>>)
                    Delegate.CreateDelegate(
                        (typeof(Func<IEnumerable<SyntaxNode>, IEnumerable<MemberDeclarationSyntax>>)), method);
                OfTypeDelegates.Add(type, f);
            }
        }

        public string GetLocationNamespace(Location location)
        {
            var ancestors = GetAncestors(location);
            var namespaceDeclaration = ancestors.OfType<NamespaceDeclarationSyntax>().First();
            string locationNamespace = namespaceDeclaration.Name.ToString();
            return locationNamespace;
        }

        public MemberDeclarationSyntax FindClosestDeclaration(Location location)
        {
            var ancestors = GetAncestors(location);

            foreach (var type in SupportedDeclarationTypes)
            {
                var typedNodes = OfTypeDelegates[type](ancestors);

                var firstNode = (MemberDeclarationSyntax) firstOrDefault(typedNodes);
                if (firstNode != null)
                    return firstNode;
            }

            throw new Exception("I dont know where this method call is...");
        }

        private static Type[] SupportedDeclarationTypes = new[] {
            typeof(MethodDeclarationSyntax),
            typeof(ConstructorDeclarationSyntax),
            typeof(PropertyDeclarationSyntax)
        };
        private static Dictionary<Type, Func<IEnumerable<SyntaxNode>, IEnumerable<MemberDeclarationSyntax>>> OfTypeDelegates;
        private static Func<IEnumerable<MemberDeclarationSyntax>, MemberDeclarationSyntax> firstOrDefault =
            (Func<IEnumerable<MemberDeclarationSyntax>, MemberDeclarationSyntax>)
            Delegate.CreateDelegate(
                (typeof(Func<IEnumerable<MemberDeclarationSyntax>, MemberDeclarationSyntax>)),
                ((Func<IEnumerable<MemberDeclarationSyntax>, MemberDeclarationSyntax>)Enumerable.FirstOrDefault).Method);

        private IEnumerable<SyntaxNode> GetAncestors(Location location)
        {
            return location.SourceTree.GetRoot().FindNode(location.SourceSpan).Ancestors();
        }
    }
}
