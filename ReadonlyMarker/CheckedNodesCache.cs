using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ReadonlyMarker
{
    public class CheckedNodesCache
    {
        private readonly Dictionary<SyntaxNode, bool> _results = new();

        public void AddCurrentNode(SyntaxNode node)
        {
            _results.Add(node, true);
        }

        public void AddNode(SyntaxNode node, bool result)
        {
            if (_results.ContainsKey(node))
                _results[node] = result;
            else
                _results.Add(node, result);
        }

        public bool TryGetValue(SyntaxNode node, out bool result)
        { 
            return _results.TryGetValue(node, out result);
        }
    }
}