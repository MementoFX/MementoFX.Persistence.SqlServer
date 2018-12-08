///source http://ryanohs.com/2016/04/generating-sql-from-expression-trees-part-2/

using MementoFX.Persistence.SqlServer.Data;
using MementoFX.Persistence.SqlServer.Helpers;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    internal static class ExpressionExtensions
    {
        public static SqlExpression ToSqlExpression<T>(this Expression<Func<T, bool>> expression, bool useCompression, bool useSingleTable)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var i = 1;
            return Recurse(ref i, useCompression, useSingleTable, expression.Body, isUnary: true);
        }

        private static SqlExpression Recurse(ref int i, bool useCompression, bool useSingleTable, Expression expression, bool isUnary = false, string prefix = null, string postfix = null)
        {
            if (expression is UnaryExpression unary)
            {
                return SqlExpression.Concat(unary.NodeType.ToSqlString(), Recurse(ref i, useCompression, useSingleTable, unary.Operand, true));
            }

            if (expression is BinaryExpression body)
            {
                return SqlExpression.Concat(Recurse(ref i, useCompression, useSingleTable, body.Left), body.NodeType.ToSqlString(), Recurse(ref i, useCompression, useSingleTable, body.Right));
            }

            if (expression is ConstantExpression constant)
            {
                var value = constant.Value;
                if (value is int)
                {
                    return SqlExpression.TextOnly(value.ToString());
                }

                if (value is string)
                {
                    value = prefix + (string)value + postfix;
                }

                if (value is bool && isUnary)
                {
                    return SqlExpression.Concat(SqlExpression.WithSingleParameter(i++, value), "=", SqlExpression.TextOnly("1"));
                }

                return SqlExpression.WithSingleParameter(i++, value);
            }

            if (expression is MemberExpression member)
            {
                var value = TryGetMemberValue(member);
                if (value != null)
                {
                    return GetMemberValue(ref i, value, prefix, postfix);
                }

                if (member.Member is PropertyInfo property)
                {
                    if (isUnary && member.Type == typeof(bool))
                    {
                        return SqlExpression.Concat(Recurse(ref i, useCompression, useSingleTable, expression), "=", SqlExpression.WithSingleParameter(i++, true));
                    }

                    var name = expression.ToString();
                    if (name.Contains('.'))
                    {
                        name = string.Join(".", name.Split('.').Skip(1));
                    }

                    name = TableHelper.GetFixedLeftPart(name, useCompression, useSingleTable);

                    return SqlExpression.TextOnly(name);
                }

                if (member.Member is FieldInfo)
                {
                    value = TryGetMemberValue(member);
                    return GetMemberValue(ref i, value, prefix, postfix);
                }

                throw new Exception($"Expression does not refer to a property or field: {expression}");
            }

            if (expression is MethodCallExpression methodCall)
            {
                // LIKE queries:
                if (methodCall.Method == typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) }))
                {
                    return SqlExpression.Concat(Recurse(ref i, useCompression, useSingleTable, methodCall.Object), "LIKE", Recurse(ref i, useCompression, useSingleTable, methodCall.Arguments[0], prefix: "%", postfix: "%"));
                }

                if (methodCall.Method == typeof(string).GetMethod(nameof(string.StartsWith), new[] { typeof(string) }))
                {
                    return SqlExpression.Concat(Recurse(ref i, useCompression, useSingleTable, methodCall.Object), "LIKE", Recurse(ref i, useCompression, useSingleTable, methodCall.Arguments[0], postfix: "%"));
                }

                if (methodCall.Method == typeof(string).GetMethod(nameof(string.EndsWith), new[] { typeof(string) }))
                {
                    return SqlExpression.Concat(Recurse(ref i, useCompression, useSingleTable, methodCall.Object), "LIKE", Recurse(ref i, useCompression, useSingleTable, methodCall.Arguments[0], prefix: "%"));
                }

                // IN queries:
                if (methodCall.Method.Name == nameof(IList.Contains))
                {
                    Expression collection;
                    Expression property;
                    if (methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 2)
                    {
                        collection = methodCall.Arguments[0];
                        property = methodCall.Arguments[1];
                    }
                    else if (!methodCall.Method.IsDefined(typeof(ExtensionAttribute)) && methodCall.Arguments.Count == 1)
                    {
                        collection = methodCall.Object;
                        property = methodCall.Arguments[0];
                    }
                    else
                    {
                        throw new Exception("Unsupported method call: " + methodCall.Method.Name);
                    }

                    var values = (IEnumerable)GetValue(collection);

                    return SqlExpression.Concat(Recurse(ref i, useCompression, useSingleTable, property), "IN", SqlExpression.WithParameters(ref i, values));
                }

                throw new Exception("Unsupported method call: " + methodCall.Method.Name);
            }

            throw new Exception("Unsupported expression: " + expression.GetType().Name);
        }

        private static SqlExpression GetMemberValue(ref int i, object value, string prefix = null, string postfix = null, bool useCompression = false)
        {
            if (value is string @string)
            {
                value = prefix + @string + postfix;
            }

            return SqlExpression.WithSingleParameter(i++, value);
        }

        private static object TryGetMemberValue(MemberExpression member)
        {
            try
            {
                return GetValue(member);
            }
            catch
            {
                return null;
            }
        }
        
        private static object GetValue(Expression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }
    }
}
