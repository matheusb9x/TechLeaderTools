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
             */
            
            string fileName = "filename.cs";
            string solutionPath = @"path\to\solution";
            string desiredNamespace = "Your.Namespace.Here";

            var solution = new SolutionExplorer(solutionPath);
            var doc = solution.GetDocument(fileName);

            var rf = solution.FindReferences(doc, new string[] { desiredNamespace }, new int[] { 24 });
            var results = rf.Results;

            foreach (var result in results)
            {
                int i = 0;

                do
                {
                    MemberDeclarationSyntax current = result.Pop();

                    if (i > 0)
                        Console.Write("-> ");

                    WriteStackItem(current);

                    i++;
                }
                while (result.Count > 0);

                Console.WriteLine(" ");
            }

            Console.ReadKey();

        }

        private static void WriteStackItem(MemberDeclarationSyntax current)
        {
            var parentIdentifier = current.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault()?.Identifier.ToString();

            var methodDeclaration = current as MethodDeclarationSyntax;
            var constructorDeclaration = current as ConstructorDeclarationSyntax;
            var classDeclaration = current as ClassDeclarationSyntax;
            var propertyDeclaration = current as PropertyDeclarationSyntax;

            // TODO: Implement this using polymorphism.
            if (methodDeclaration != null)
            {
                Console.WriteLine(parentIdentifier + "." + methodDeclaration.Identifier.ToString() + methodDeclaration.ParameterList.ToString());
            }
            else if (constructorDeclaration != null)
            {
                Console.WriteLine(parentIdentifier + "." + constructorDeclaration.Identifier.ToString() + constructorDeclaration.ParameterList.ToString());
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
