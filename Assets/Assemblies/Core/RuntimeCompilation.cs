using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Larnix.Core
{
    public static class RuntimeCompilation
    {
        public static Func<object[], object> CompileConstructor(ConstructorInfo constructor)
        {
            if (constructor == null)
                throw new ArgumentNullException(nameof(constructor));

            ParameterInfo[] parameters = constructor.GetParameters();
            ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

            Expression[] paramExpressions = new Expression[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Expression paramAccess = Expression.ArrayIndex(argsParam, index);
                paramExpressions[i] = Expression.Convert(paramAccess, parameters[i].ParameterType);
            }

            NewExpression newExpr = Expression.New(constructor, paramExpressions);

            Expression<Func<object[], object>> lambda = Expression.Lambda<Func<object[], object>>(Expression.Convert(newExpr, typeof(object)), argsParam);

            return lambda.Compile();
        }

        public static Func<object, object[], object> CompileMethod(MethodInfo method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
            ParameterExpression argsParam = Expression.Parameter(typeof(object[]), "args");

            ParameterInfo[] parameters = method.GetParameters();
            Expression[] paramExpressions = new Expression[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Expression paramAccess = Expression.ArrayIndex(argsParam, index);
                paramExpressions[i] = Expression.Convert(paramAccess, parameters[i].ParameterType);
            }

            Expression instanceExpr = method.IsStatic ? null : Expression.Convert(instanceParam, method.DeclaringType);

            MethodCallExpression callExpr = Expression.Call(instanceExpr, method, paramExpressions);

            Expression body = method.ReturnType == typeof(void)
                ? Expression.Block(callExpr, Expression.Constant(null, typeof(object)))
                : Expression.Convert(callExpr, typeof(object));

            Expression<Func<object, object[], object>> lambda = Expression.Lambda<Func<object, object[], object>>(body, instanceParam, argsParam);

            return lambda.Compile();
        }
    }
}
