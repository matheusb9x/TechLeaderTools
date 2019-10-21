using Mahayana;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp3
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
             * Some thoughts:
             * The stack trace does not bring property getters and setters.
             * There must be a way to filter out some tokens (don't find references for this tokens) to avoid paths that are too much common and pollute results
             */

            string fileName = "";
            string solutionPath = "";
            string[] desiredNamespaces = new string[0];

            
            var solution = new SolutionExplorer(solutionPath);
            var doc = solution.GetDocument(fileName);

            var rf = solution.FindReferences(doc, desiredNamespaces, new int[] { 35 });
            var results = rf.Results;

            foreach (var result in results)
            {
                int i = 0;

                foreach (var current in result) {

                    if (i > 0)
                        Console.Write("-> ");

                    WriteStackItem(current);

                    i++;
                }

                Console.WriteLine(" ");
            }

            Console.ReadKey();

        }

        private static void WriteStackItem(MemberDeclarationSyntax current)
        {
            var namespaceIdentifier = current.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()?.Name.ToString();
            var parentIdentifier = current.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ToString();

            var methodDeclaration = current as MethodDeclarationSyntax;
            var constructorDeclaration = current as ConstructorDeclarationSyntax;
            var classDeclaration = current as ClassDeclarationSyntax;
            var propertyDeclaration = current as PropertyDeclarationSyntax;

            // TODO: Implement this using polymorphism.
            if (methodDeclaration != null)
            {
                Console.WriteLine(namespaceIdentifier + "." + parentIdentifier + "." + methodDeclaration.Identifier.ToString() + methodDeclaration.ParameterList.ToString());
            }
            else if (constructorDeclaration != null)
            {
                Console.WriteLine(namespaceIdentifier + "." + parentIdentifier + "." + constructorDeclaration.Identifier.ToString() + constructorDeclaration.ParameterList.ToString());
            }
            else if (classDeclaration != null)
            {
                Console.WriteLine("class " + classDeclaration.Identifier.ToString());
            }
            else if (propertyDeclaration != null)
            {
                Console.WriteLine(((ClassDeclarationSyntax)propertyDeclaration.Parent).Identifier.ValueText + "." + propertyDeclaration.Identifier.ValueText);
            }
        }

        private static void Workspace_WorkspaceFailed(object sender, Microsoft.CodeAnalysis.WorkspaceDiagnosticEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
