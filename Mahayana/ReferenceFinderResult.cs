using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Collections.Generic;

namespace Mahayana
{
    public class ReferenceFinderResult : IEnumerable<ReferenceFinderResult.Item>
    {
        public ReferenceFinderResult()
        {
            this.results = new HashSet<Item>();
        }

        public IEnumerator<Item> GetEnumerator()
        {
            return results.GetEnumerator();
        }

        public void Add(Item item)
        {
            results.Add(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return results.GetEnumerator();
        }

        private HashSet<Item> results;

        public class Item : IEnumerable<MemberDeclarationSyntax> {
            public Item()
            {
                this.stackTrace = new Stack<MemberDeclarationSyntax>();
            }

            public void Add(MemberDeclarationSyntax item)
            {
                stackTrace.Push(item);
            }

            public Item Clone()
            {
                var clone = new Item();
                clone.stackTrace = new Stack<MemberDeclarationSyntax>(new Stack<MemberDeclarationSyntax>(stackTrace));
                return clone;
            }

            public IEnumerator<MemberDeclarationSyntax> GetEnumerator()
            {
                return stackTrace.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return stackTrace.GetEnumerator();
            }

            private Stack<MemberDeclarationSyntax> stackTrace;
        }
    }
}
