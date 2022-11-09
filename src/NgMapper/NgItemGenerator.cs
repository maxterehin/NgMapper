using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NgMapper.Helper;

namespace NgMapper
{
	public class NgItemGenerator
	{
		private readonly Compilation _compilation;
		private readonly ImmutableArray<ClassDeclarationSyntax> _classess;
		private readonly SourceProductionContext _context;

		public NgItemGenerator(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classess, SourceProductionContext spc)
		{
			_compilation = compilation;
			_classess = classess;
			_context = spc;
		}

		public IReadOnlyList<NgItem> Generate()
		{
			var models = new List<NgItem>();

			foreach (var cls in _classess)
			{
				_context.CancellationToken.ThrowIfCancellationRequested();
				models.AddRange(GetItems(cls));
			}

			return models;
		}

		private IReadOnlyList<NgItem> GetItems(ClassDeclarationSyntax cls)
		{
			var semanticModel = _compilation.GetSemanticModel(cls.SyntaxTree);
			var configSyntax = GetConfigureMethodFromClass(cls);
			var result = new List<NgItem>();

			if (cls.BaseList is not null && cls.BaseList.ToString().Contains(nameof(INgCommonMapper)))
			{
				var chooseTypeMethodsSyntax = GetMethodCallsByName(configSyntax, nameof(NgMapCreator.ChooseType));

				foreach (var methodSyntax in chooseTypeMethodsSyntax)
				{
					var baseTypeSyntax = (methodSyntax as GenericNameSyntax)?.TypeArgumentList.Arguments[0];
					if (baseTypeSyntax is null)
					{
						continue;
					}
					var baseTypeInfo = semanticModel.GetTypeInfo(baseTypeSyntax);
					if (baseTypeInfo.Type is INamedTypeSymbol baseTypeSymbol)
					{
						var chooseTypeArguments = GetArgumentsFromIdentifier(methodSyntax);
						var nodeSyntax = chooseTypeArguments?[0];
						if (nodeSyntax is null)
						{
							continue;
						}
						result.AddRange(GetModelsFromSyntax(baseTypeSymbol, nodeSyntax, semanticModel));
					};
				}
			}
			else
			{
				var baseTypeSymbol = semanticModel.GetDeclaredSymbol(cls);
				if (baseTypeSymbol is not null && configSyntax is not null)
				{
					result.AddRange(GetModelsFromSyntax(baseTypeSymbol, configSyntax, semanticModel));
				}
			}

			return result;
		}

		private static MethodDeclarationSyntax? GetMapCreatorConfig(ClassDeclarationSyntax classDeclaration)
		{
			var config = classDeclaration.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault(x => x.Identifier.ValueText == nameof(INgCommonMapper.Configure));

			return config;
		}

		private List<SimpleNameSyntax> GetMethodCallsByName(SyntaxNode? node, string methodName)
		{
			var methodNodes = node is null ? new() : node.DescendantNodes()
				.OfType<SimpleNameSyntax>()
				.Where(x => x.Identifier.ValueText == methodName).ToList();

			return methodNodes;
		}

		private IReadOnlyList<NgItem> GetModelsFromSyntax(INamedTypeSymbol baseType, SyntaxNode node, SemanticModel semanticModel)
		{
			var mapTos = GetMethodCallsByName(node, nameof(NgMapConfig<object>.MapTo));
			var mapFroms = GetMethodCallsByName(node, nameof(NgMapConfig<object>.MapFrom));

			var models = new List<NgItem>();
			models.AddRange(mapTos.Select(m => ProcessMapTo(m, baseType, semanticModel)).Where(x => x is not null)!);

			models.AddRange(mapFroms.Select(m => ProcessMapFrom(m, baseType, semanticModel)).Where(x => x is not null)!);

			return models;
		}

		private MethodDeclarationSyntax? GetConfigureMethodFromClass(SyntaxNode node)
		{
			var config = node.DescendantNodes()
				.OfType<MethodDeclarationSyntax>()
				.FirstOrDefault(x => x.Identifier.ValueText == nameof(INgMapper<object>.Configure));

			return config;
		}

		private NgItem? ProcessMapTo(SimpleNameSyntax mapSyntax, INamedTypeSymbol sourceTypeSymbol, SemanticModel semanticModel)
		{
			if (mapSyntax is not GenericNameSyntax dstTypeSyntax)
			{
				return default;
			}

			var dstTypeInfo = semanticModel.GetTypeInfo(dstTypeSyntax.TypeArgumentList.Arguments[0]);

			if (dstTypeInfo.Type is not INamedTypeSymbol dstTypeSymbol)
			{
				return default;
			};

			return ProcessMapSyntax(mapSyntax, sourceTypeSymbol, dstTypeSymbol, semanticModel);
		}

		private NgItem? ProcessMapFrom(SimpleNameSyntax mapSyntax, INamedTypeSymbol dstTypeSymbol, SemanticModel semanticModel)
		{
			if (mapSyntax is not GenericNameSyntax sourceTypeSyntax)
			{
				return default;
			}

			var sourceTypeInfo = semanticModel.GetTypeInfo(sourceTypeSyntax.TypeArgumentList.Arguments[0]);

			if (sourceTypeInfo.Type is not INamedTypeSymbol sourceTypeSymbol)
			{
				return default;
			};

			return ProcessMapSyntax(mapSyntax, sourceTypeSymbol, dstTypeSymbol, semanticModel);
		}

		private NgItem? ProcessMapSyntax(SimpleNameSyntax mapSyntax, INamedTypeSymbol sourceTypeSymbol, INamedTypeSymbol dstTypeSymbol, SemanticModel semanticModel)
		{
			var model = new NgItem(sourceTypeSymbol, dstTypeSymbol);

			//find ignore calls
			var ignoreCalls = GetMethodCallsInLambda(mapSyntax, nameof(NgMapSetting<object, object>.Ignore));

			foreach (var lambdaIdentifier in ignoreCalls)
			{
				var args = GetArgumentsFromIdentifier(lambdaIdentifier);
				if (args is null)
				{
					return default;
				}
				var property = ParsePropertyName(args[0]);
				var ignoredProperty = sourceTypeSymbol.GetMembers(property)
					.OfType<IPropertySymbol>()
					.SingleOrDefault();
				model.AddIgnoredProperty(ignoredProperty);
			}

			//Find constructor initialization
			var constructorCall = GetMethodCallsInLambda(mapSyntax, nameof(NgMapSetting<object, object>.Init)).SingleOrDefault();
			if (constructorCall is not null)
			{
				var args = GetArgumentsFromIdentifier(constructorCall);
				if (args is null)
				{
					return default;
				}
				model.Constructor = args[0];
			}

			var forMemberCalls = GetMethodCallsInLambda(mapSyntax, nameof(NgMapSetting<object, object>.ForMember));
			foreach (var forMemberCall in forMemberCalls)
			{
				var args = GetArgumentsFromIdentifier(forMemberCall);
				if (args is null)
				{
					return default;
				}
				var property = ParsePropertyName(args[0]);
				var callBack = args[1];
				//find property in destination type
				var customProperty = dstTypeSymbol.GetMembers(property)
					.OfType<IPropertySymbol>()
					.SingleOrDefault();

				model.AddCustomProperty(customProperty, callBack);
			}

			return model;
		}

		private List<SimpleNameSyntax> GetMethodCallsInLambda(SyntaxNode node, string methodName)
		{
			if (node?.Parent?.Parent is InvocationExpressionSyntax invocation)
			{
				var lambdaSyntax = invocation?.ArgumentList?.Arguments[0];

				if (lambdaSyntax is not null)
				{
					return GetMethodCallsByName(lambdaSyntax, methodName);
				}
			}

			return new();
		}

		private static IReadOnlyList<ArgumentSyntax>? GetArgumentsFromIdentifier(SimpleNameSyntax identifier)
		{
			var invocation = identifier.Parent?.Parent as InvocationExpressionSyntax;
			return invocation?.ArgumentList?.Arguments;
		}
		private static string ParsePropertyName(SyntaxNode lambdaIdentifierNode)
		{
			var prop = lambdaIdentifierNode.DescendantNodes().OfType<IdentifierNameSyntax>().LastOrDefault();
			return prop.Identifier.ValueText;
		}
	}
}
