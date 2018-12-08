///source http://ryanohs.com/2016/04/generating-sql-from-expression-trees-part-2/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MementoFX.Persistence.SqlServer.Data
{
    internal class SqlExpression
    {
        public string CommandText { get; }
        public Dictionary<string, object> Parameters { get; }
        
        SqlExpression(string commandText, Dictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(commandText))
            {
                throw new ArgumentException(nameof(commandText));
            }

            this.CommandText = commandText;
            this.Parameters = parameters ?? new Dictionary<string, object>();
        }

        public static SqlExpression TextOnly(string commandText)
        {
            return new SqlExpression(commandText);
        }

        public static SqlExpression WithSingleParameter(int counter, object value)
        {
            var parameterName = string.Format(Commands.ParameterNameFormat, counter);

            var parameters = new Dictionary<string, object>
            {
                { parameterName, value }
            };

            return new SqlExpression(parameterName, parameters);
        }

        public static SqlExpression WithParameters(ref int counter, IEnumerable values)
        {
            var parameters = new Dictionary<string, object>();

            var stringBuilder = new StringBuilder();

            foreach (var value in values)
            {
                var parameterName = string.Format(Commands.ParameterNameFormat, counter);
                parameters.Add(parameterName, value);
                stringBuilder.Append(parameterName + ",");
                counter++;
            }

            if (stringBuilder.Length == 1)
            {
                stringBuilder.Append("NULL" + ",");
            }

            var commandText = stringBuilder.ToString();
            if (commandText.EndsWith(","))
            {
                commandText = commandText.Substring(0, commandText.Length - 1);
            }
            
            return new SqlExpression(Commands.Enclose(commandText), parameters);
        }

        public static SqlExpression Concat(string @operator, SqlExpression operand)
        {
            return new SqlExpression(Commands.Enclose(@operator, operand.CommandText), operand.Parameters);
        }

        public static SqlExpression Concat(SqlExpression left, string @operator, SqlExpression right)
        {
            var parameters = left.Parameters.Union(right.Parameters).ToDictionary();

            return new SqlExpression(Commands.Enclose(left.CommandText, @operator, right.CommandText), parameters);
        }
    }
}
