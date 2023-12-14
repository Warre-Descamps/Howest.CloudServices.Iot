using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Howest.Mct.Functions.CosmosDb.Helper;

internal static class ExpressionHelper
{
    /// <summary>
    /// https://stackoverflow.com/a/53677055
    /// </summary>
    private sealed class PredicateVisitor : ExpressionVisitor
    {
        public List<string?> Components { get; } = new();
    
        protected override Expression VisitBinary(BinaryExpression node)
        {
            Visit(node.Left);
    
            Components.Add(GetOperator(node.NodeType));
    
            Visit(node.Right);
    
            return node;
        }
    
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node is { Member: FieldInfo fieldInfo, Expression: ConstantExpression constExpr })
            {
                Components.Add(fieldInfo.GetValue(constExpr.Value)?.ToString());
            }
            else
            {
                Visit(node.Expression);
                Components.Add($"c['{node.Member.Name}']");
            }
    
            return node;
        }
    
        protected override Expression VisitConstant(ConstantExpression node)
        {
            Components.Add($"'{node.Value}'");
    
            return node;
        }
    
        private static string GetOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.Not => "!",
                ExpressionType.NotEqual => "!=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.Or => "or",
                ExpressionType.OrElse => "or",
                ExpressionType.And => "and",
                ExpressionType.AndAlso => "and",
                ExpressionType.Add => "+",
                ExpressionType.AddAssign => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.SubtractAssign => "-",
                _ => "???"
            };
        }
    }

    public static string GetCosmosDbPredicate<T>(Expression<Func<T, bool>> predicate)
    {
        var visitor = new PredicateVisitor();
        visitor.Visit(predicate);
        return string.Join(" ", visitor.Components);
    }
}
