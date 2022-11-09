using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NgMapper
{
	public record NgItem
	{
		private readonly HashSet<IPropertySymbol> ignoredProperties = new(SymbolEqualityComparer.Default);

		public Dictionary<IPropertySymbol, ArgumentSyntax> CustomProperties { get; private set; } = new(SymbolEqualityComparer.Default);

		public readonly INamedTypeSymbol MapFromClass;

		public readonly INamedTypeSymbol MapToClass;

		public ArgumentSyntax? Constructor { get; set; }

		public NgItem(INamedTypeSymbol fromClass, INamedTypeSymbol toClass)
		{
			MapFromClass = fromClass;
			MapToClass = toClass;
		}

		//todo: check if target class contains props
		public IReadOnlyList<IPropertySymbol> GetSourceTypeProperties
		{
			get => MapFromClass.GetMembers()
					.OfType<IPropertySymbol>()
					.Where(p => p.DeclaredAccessibility != Accessibility.Private)
					.Where(p => !IsPropertyIgnored(p))
					.Where(p => !IsCustomProperty(p))
					.ToList();
		}

		public bool IsPropertyIgnored(IPropertySymbol prop) => ignoredProperties.Contains(prop);

		public void AddIgnoredProperty(IPropertySymbol prop)
		{
			ignoredProperties.Add(prop);
		}

		public bool IsCustomProperty(IPropertySymbol prop) => CustomProperties.ContainsKey(prop);

		public void AddCustomProperty(IPropertySymbol prop, ArgumentSyntax func)
		{
			CustomProperties.Add(prop, func);
		}
	}
}
