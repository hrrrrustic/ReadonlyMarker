using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ReadonlyMarker
{
    public class MatchedNodes
    {
        public readonly IReadOnlyCollection<MethodDeclarationSyntax> NonReadonlyMethods;
        public readonly IReadOnlyCollection<AccessorDeclarationSyntax> NonReadonlyGetters;
        public readonly IReadOnlyCollection<PropertyDeclarationSyntax> ArrowedProperties;
        public readonly IReadOnlyCollection<IndexerDeclarationSyntax> Indexers;

        public MatchedNodes(IReadOnlyCollection<MethodDeclarationSyntax> nonReadonlyMethods, IReadOnlyCollection<AccessorDeclarationSyntax> nonReadonlyGetters, IReadOnlyCollection<PropertyDeclarationSyntax> arrowedProperties, IReadOnlyCollection<IndexerDeclarationSyntax> indexers)
        {
            NonReadonlyMethods = nonReadonlyMethods;
            NonReadonlyGetters = nonReadonlyGetters;
            ArrowedProperties = arrowedProperties;
            Indexers = indexers;
        }
    }
}