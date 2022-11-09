using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NgMapper
{
	public class NgCodeGenerator
	{
		private readonly IReadOnlyList<NgItem> _items;

		public NgCodeGenerator(IReadOnlyList<NgItem> items)
		{
			_items = items;
		}

		public string GenerateExtensionClass()
		{
			var sb = new StringBuilder();
			sb.Append("using System;");

			var groupedModels = _items.GroupBy(x => (ISymbol)x.MapFromClass, SymbolEqualityComparer.Default);
			foreach (var group in groupedModels)
			{
				var fromClass = group.Key;
				sb.AppendLine($"namespace {fromClass.ContainingNamespace.ToDisplayString()}").AppendLine("{");
				var acc = GetAccessibility(fromClass.DeclaredAccessibility);

				sb.AppendLine($"{acc} static class {fromClass.Name}NgMapperExtensions").AppendLine("{");
				foreach (var model in group)
				{
					var toClass = model.MapToClass;

					sb.AppendLine($"public static {toClass.Name} MapTo{toClass.Name}(this {fromClass.Name} source)").AppendLine("{");

					sb.AppendLine(GenerateConstructor(model));

					foreach (var prop in model.GetSourceTypeProperties)
					{
						sb.AppendLine($"result.{prop.Name} = source.{prop.Name};");
					}

					sb.AppendLine(GenerateCustomProperties(model));

					sb.AppendLine("return result;");
					sb.AppendLine("}");
				}

				sb.AppendLine("}").AppendLine("}");
			}

			return sb.ToString();
		}

		private string GenerateCustomProperties(NgItem model)
		{
			var sb = new StringBuilder();

			foreach (var customProperty in model.CustomProperties)
			{
				var prop = customProperty.Key;
				var argument = customProperty.Value;

				if (argument.ChildNodes().FirstOrDefault() is LambdaExpressionSyntax lambda)
				{
					sb.AppendLine($"Func<{model.MapToClass.Name}, {model.MapFromClass.Name}, {prop.Type.Name}> {prop.Name}Lambda = {lambda};");
					sb.AppendLine($"result.{prop.Name} = {prop.Name}Lambda(result, source);");
				}
				else
				{
					sb.AppendLine($"result.{prop.Name} = {argument}(result, source);");
				}
			}

			return sb.ToString();
		}


		private string GenerateConstructor(NgItem model)
		{
			if (model.Constructor is ArgumentSyntax argument)
			{
				var sb = new StringBuilder();
				if (argument.ChildNodes().FirstOrDefault() is LambdaExpressionSyntax lambda)
				{
					sb.AppendLine($"Func<{model.MapFromClass.Name}, {model.MapToClass.Name}> constructorCreator = {lambda};");
					sb.Append("var result = constructorCreator(source);");
				}
				else
				{
					sb.Append($"var result = {argument}(source);");
				}

				return sb.ToString();
			}
			else
			{
				return $"var result = new {model.MapToClass.Name}();";
			}
		}

		private static string GetAccessibility(Accessibility declaredAccessibility) => declaredAccessibility == Accessibility.Public ? "public" : "internal";
	}
}
