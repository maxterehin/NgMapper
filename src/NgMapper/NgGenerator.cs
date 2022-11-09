using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NgMapper;
using NgMapper.Helper;

namespace ngMapper
{
	[Generator]
	public class NgGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			IncrementalValuesProvider<ClassDeclarationSyntax> classSyntax = context.SyntaxProvider.CreateSyntaxProvider(
					predicate: static (s, _) => IsMappableClass(s),
					transform: static (ctx, _) => ctx.Node as ClassDeclarationSyntax
				)
				.Where(static cls => cls is not null)!;

			var compilationAndClassess = context.CompilationProvider.Combine(classSyntax.Collect());

			context.RegisterSourceOutput(compilationAndClassess, static (spc, source) => GenerateSourceCode(source.Left, source.Right, spc));
		}

		static bool IsMappableClass(SyntaxNode syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax cds && DerivesFromIMappable(cds))
			{
				return true;
			}

			return false;
		}

		static bool DerivesFromIMappable(ClassDeclarationSyntax cds)
		{
			var baseList = cds.BaseList?.ToString() ?? string.Empty;
			return baseList.Contains(nameof(INgMapper<object>)) || baseList.Contains(nameof(INgCommonMapper));
		}

		private static void GenerateSourceCode(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classess, SourceProductionContext spc)
		{
			if (classess.IsDefaultOrEmpty)
			{
				return;
			}

			var generator = new NgItemGenerator(compilation, classess, spc);
			var items = generator.Generate();

			if (items.Any())
			{
				var sourceCodeGen = new NgCodeGenerator(items);
				var result = sourceCodeGen.GenerateExtensionClass();

				spc.AddSource("NgMapper.Extensions.g.cs", SourceText.From(result, Encoding.UTF8));
			}
		}
	}
}
