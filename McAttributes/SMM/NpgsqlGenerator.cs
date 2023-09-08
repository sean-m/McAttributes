
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace SMM
{
	public class NpgsqlGenerator : McRule.ExpressionGeneratorBase {
		/// <summary> 
		/// Builds expressions using string member functions StartsWith, EndsWith or Contains as the comparator. 
		/// </summary> 
		public override Expression<Func<T, bool>> AddStringPropertyExpression<T>(
			Expression<Func<T, string>> lambda, string filter, string filterType, bool ignoreCase = false) {
#if DEBUG
			if (!(filterType == "StartsWith" || filterType == "EndsWith" || filterType == "Contains" ||
				  filterType == "Equals")) {
				throw new Exception($"filterType must equal StartsWith, EndsWith or Contains. Passed: {filterType}");
			}
#endif
			// Check that the property isn't null, otherwise we'd hit null object exceptions at runtime
			var notNull = Expression.NotEqual(lambda.Body, Expression.Constant(null));

			// Setup calls to: StartsWith, EndsWith, Contains, or Equals,
			// conditionally using character case neutral comparision.
			List<Expression> expressionArgs = new List<Expression>() { Expression.Constant(filter, typeof(string)) };
			MethodInfo methodInfo;
			Expression strPredicate;

			if (ignoreCase) {
				var ilike = typeof(NpgsqlDbFunctionsExtensions).GetMethod("ILike",
					BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					new[] {
						typeof(DbFunctions),
						typeof(string),
						typeof(string)
					},
					null
					);

				switch (filterType) {
					case "StartsWith":
						filter = $"{filter}%";
						break;
					case "EndsWith":
						filter = $"%{filter}";
						break;
				}


				var bodyLike = Expression.Call(ilike, Expression.Constant(null, typeof(DbFunctions)),
					lambda.Body, Expression.Constant(filter, typeof(string)));

				return Expression.Lambda<Func<T, bool>>(bodyLike, Expression.Parameter(typeof(T)));
			} else {
				methodInfo = typeof(string).GetMethod(filterType, new[] { typeof(string) });

				strPredicate = Expression.Call(lambda.Body, methodInfo, expressionArgs);
			}


			Expression filterExpression = Expression.AndAlso(notNull, strPredicate);

			return Expression.Lambda<Func<T, bool>>(
				filterExpression,
				lambda.Parameters);
		}
	}
}
