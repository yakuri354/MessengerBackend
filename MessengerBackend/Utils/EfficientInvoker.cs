// MIT License
//
// Copyright (c) 2016 Tom DuPont
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// This code contains minor modifications

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MessengerBackend.Utils.Tact.Reflection;

#nullable disable
namespace MessengerBackend.Utils
{
    namespace Tact.Reflection
    {
        internal sealed class EfficientInvoker
        {
            private static readonly ConcurrentDictionary<ConstructorInfo, Func<object[], object>>
                ConstructorToWrapperMap
                    = new ConcurrentDictionary<ConstructorInfo, Func<object[], object>>();

            private static readonly ConcurrentDictionary<Type, EfficientInvoker> TypeToWrapperMap
                = new ConcurrentDictionary<Type, EfficientInvoker>();

            private static readonly ConcurrentDictionary<MethodKey, EfficientInvoker> MethodToWrapperMap
                = new ConcurrentDictionary<MethodKey, EfficientInvoker>(MethodKeyComparer.Instance);

            private readonly Func<object, object[], object> _func;

            public EfficientInvoker(Func<object, object[], object> func) => _func = func;

            public static Func<object[], object> ForConstructor(ConstructorInfo constructor)
            {
                if (constructor == null)
                {
                    throw new ArgumentNullException(nameof(constructor));
                }

                return ConstructorToWrapperMap.GetOrAdd(constructor, t =>
                {
                    CreateParamsExpressions(constructor, out var argsExp, out var paramsExps);

                    var newExp = Expression.New(constructor, paramsExps);
                    var resultExp = Expression.Convert(newExp, typeof(object));
                    var lambdaExp = Expression.Lambda(resultExp, argsExp);
                    var lambda = lambdaExp.Compile();
                    return (Func<object[], object>) lambda;
                });
            }

            public static EfficientInvoker ForDelegate(Delegate del)
            {
                if (del == null)
                {
                    throw new ArgumentNullException(nameof(del));
                }

                var type = del.GetType();
                return TypeToWrapperMap.GetOrAdd(type, t =>
                {
                    var method = del.GetMethodInfo();
                    var wrapper = CreateMethodWrapper(t, method, true);
                    return new EfficientInvoker(wrapper);
                });
            }

            public static EfficientInvoker ForMethod(Type type, string methodName)
            {
                if (type == null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                if (methodName == null)
                {
                    throw new ArgumentNullException(nameof(methodName));
                }

                var key = new MethodKey(type, methodName);
                return MethodToWrapperMap.GetOrAdd(key, k =>
                {
                    var method = k.Type.GetTypeInfo().GetMethod(k.Name);
                    var wrapper = CreateMethodWrapper(k.Type, method, false);
                    return new EfficientInvoker(wrapper);
                });
            }

            public static EfficientInvoker ForProperty(Type type, string propertyName)
            {
                if (type == null)
                {
                    throw new ArgumentNullException(nameof(type));
                }

                if (propertyName == null)
                {
                    throw new ArgumentNullException(nameof(propertyName));
                }

                var key = new MethodKey(type, propertyName);
                return MethodToWrapperMap.GetOrAdd(key, k =>
                {
                    var wrapper = CreatePropertyWrapper(type, propertyName);
                    return new EfficientInvoker(wrapper);
                });
            }

            public object Invoke(object target, params object[] args) => _func(target, args);

            public async Task<object> InvokeAsync(object target, params object[] args)
            {
                var result = _func(target, args);
                if (!(result is Task task))
                {
                    return result;
                }

                if (!task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                }

                return task.GetResult();
            }

            public async Task<T> InvokeGenericAsync<T>(object target, params object[] args) =>
                _func(target, args) switch
                {
                    Task<T> task => await task.ConfigureAwait(false),
                    T sync => sync,
                    _ => throw new ArgumentException($"Method returns something other than {typeof(T)}")
                };

            public static Func<object, object[], object> CreateMethodWrapper(Type type, MethodInfo method,
                bool isDelegate)
            {
                CreateParamsExpressions(method, out var argsExp, out var paramsExps);

                var targetExp = Expression.Parameter(typeof(object), "target");
                var castTargetExp = Expression.Convert(targetExp, type);
                var invokeExp = isDelegate
                    ? (Expression) Expression.Invoke(castTargetExp, paramsExps)
                    : Expression.Call(castTargetExp, method, paramsExps);

                LambdaExpression lambdaExp;

                if (method.ReturnType != typeof(void))
                {
                    var resultExp = Expression.Convert(invokeExp, typeof(object));
                    lambdaExp = Expression.Lambda(resultExp, targetExp, argsExp);
                }
                else
                {
                    var constExp = Expression.Constant(null, typeof(object));
                    var blockExp = Expression.Block(invokeExp, constExp);
                    lambdaExp = Expression.Lambda(blockExp, targetExp, argsExp);
                }

                var lambda = lambdaExp.Compile();
                return (Func<object, object[], object>) lambda;
            }

            private static void CreateParamsExpressions(MethodBase method, out ParameterExpression argsExp,
                out Expression[] paramsExps)
            {
                var parameters = method.GetParameterTypes();

                argsExp = Expression.Parameter(typeof(object[]), "args");
                paramsExps = new Expression[parameters.Count];

                for (var i = 0; i < parameters.Count; i++)
                {
                    var constExp = Expression.Constant(i, typeof(int));
                    var argExp = Expression.ArrayIndex(argsExp, constExp);
                    paramsExps[i] = Expression.Convert(argExp, parameters[i]);
                }
            }

            private static Func<object, object[], object> CreatePropertyWrapper(Type type, string propertyName)
            {
                var property = type.GetRuntimeProperty(propertyName);
                var targetExp = Expression.Parameter(typeof(object), "target");
                var argsExp = Expression.Parameter(typeof(object[]), "args");
                var castArgExp = Expression.Convert(targetExp, type);
                var propExp = Expression.Property(castArgExp, property!);
                var castPropExp = Expression.Convert(propExp, typeof(object));
                var lambdaExp = Expression.Lambda(castPropExp, targetExp, argsExp);
                var lambda = lambdaExp.Compile();
                return (Func<object, object[], object>) lambda;
            }

            private class MethodKeyComparer : IEqualityComparer<MethodKey>
            {
                public static readonly MethodKeyComparer Instance = new MethodKeyComparer();

                public bool Equals(MethodKey x, MethodKey y) =>
                    x.Type == y.Type &&
                    StringComparer.Ordinal.Equals(x.Name, y.Name);

                public int GetHashCode(MethodKey obj)
                {
                    var typeCode = obj.Type.GetHashCode();
                    var methodCode = obj.Name.GetHashCode();
                    return CombineHashCodes(typeCode, methodCode);
                }

                // From System.Web.Util.HashCodeCombiner
                private static int CombineHashCodes(int h1, int h2) => ((h1 << 5) + h1) ^ h2;
            }

            private struct MethodKey
            {
                public MethodKey(Type type, string name)
                {
                    Type = type;
                    Name = name;
                }

                public readonly Type Type;
                public readonly string Name;
            }
        }
    }

    internal static class EfficientExtensions
    {
        private const string CompleteTaskMessage = "Task must be complete";

        private const string ResultPropertyName = "Result";

        private static readonly Type GenericTaskType = typeof(Task<>);

        private static readonly ConcurrentDictionary<Type, bool> GenericTaskTypeMap =
            new ConcurrentDictionary<Type, bool>();

        internal static readonly ConcurrentDictionary<MethodBase, IReadOnlyList<Type>> ParameterMap =
            new ConcurrentDictionary<MethodBase, IReadOnlyList<Type>>();

        public static EfficientInvoker GetMethodInvoker(this Type type, string methodName) =>
            EfficientInvoker.ForMethod(type, methodName);

        public static EfficientInvoker GetPropertyInvoker(this Type type, string propertyName) =>
            EfficientInvoker.ForProperty(type, propertyName);

        public static object GetResult(this Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            if (!task.IsCompleted)
            {
                throw new ArgumentException(CompleteTaskMessage, nameof(task));
            }

            var type = task.GetType();
            var isGenericTaskType = GenericTaskTypeMap.GetOrAdd(type,
                t => t.GetGenericTypeDefinition() == GenericTaskType);

            return isGenericTaskType
                ? type.GetPropertyInvoker(ResultPropertyName).Invoke(task)
                : null;
        }

        public static IReadOnlyList<Type> GetParameterTypes(this MethodBase method)
        {
            return ParameterMap.GetOrAdd(method, c =>
                c.GetParameters().Select(p => p.ParameterType).ToArray());
        }
    }

    public class EfficientReflectionDelegate<T>
    {
        private readonly EfficientInvoker _invoker;

        protected EfficientReflectionDelegate(
            Type type,
            MethodInfo methodInfo,
            bool authenticated)
        {
            Info = methodInfo;
            _invoker = new EfficientInvoker(EfficientInvoker
                .CreateMethodWrapper(type, methodInfo, false));
        }

        public MethodInfo Info { get; }

        protected async Task<T> Invoke(object target, object[] parameters) =>
            await _invoker.InvokeGenericAsync<T>(target, parameters).ConfigureAwait(false);
    }
}