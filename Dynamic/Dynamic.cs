using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Reflection;
using System.Linq.Expressions;

namespace DynamicExtensions
{
    class Dynamic : RealProxy, IRemotingTypeInfo
    {
        Dictionary<string, Func<object[], object>> m_methods;

        public Dynamic()
            : base(typeof(MarshalByRefObject))
        {
            this.TypeName = this.GetType().Name;

            m_methods = new Dictionary<string, Func<object[], object>>();
        }

        public Dynamic(object baseObj)
            : this()
        {
            MixIn(baseObj, false);
        }

        private void MixIn(object inst, bool overwrite)
        {
            var type = inst.GetType();

            foreach (var method in type.GetMethods().Where(m => !m.IsGenericMethod))
            {
                var key = MakeMethodKey(method);

                if (!m_methods.ContainsKey(key))
                    m_methods.Add(key, MakeMethodCallFunc(method, inst));
                else
                    m_methods[key] = MakeMethodCallFunc(method, inst);
            }

            if (type.Name.StartsWith("<>"))
            {
                //We are dealing w/ an Anon Type
                foreach (var prop in type.GetProperties())
                {
                    var getKey = String.Format("get_{0}", prop.Name.ToLower());
                    var setKey = String.Format("set_{0}_{1}", prop.Name, prop.PropertyType.FullName).ToLower();

                    Reference valRef = new Reference() { Value = prop.GetValue(inst, null) };

                    Func<object[], object> get = args => valRef.Value ;
                    Func<object[], object> set = args => {
                        valRef.Value = args[0];
                        return valRef.Value;
                    };

                    if (m_methods.ContainsKey(getKey))
                        m_methods[getKey] = get;
                    else
                        m_methods.Add(getKey, get);

                    if (m_methods.ContainsKey(setKey))
                        m_methods[setKey] = set;
                    else
                        m_methods.Add(setKey, set);
                }
            }
            else
            {
                foreach (var prop in type.GetProperties())
                {
                    var getter = prop.GetGetMethod();
                    var setter = prop.GetSetMethod(true);

                    if (getter != null)
                    {
                        var key = MakeMethodKey(getter);

                        if (!m_methods.ContainsKey(key))
                            m_methods.Add(key, MakeMethodCallFunc(getter, inst));
                        else if (overwrite)
                            m_methods[key] = MakeMethodCallFunc(getter, inst);
                    }

                    if (setter != null)
                    {
                        var key = MakeMethodKey(setter);

                        if (!m_methods.ContainsKey(key))
                            m_methods.Add(key, MakeMethodCallFunc(setter, inst));
                        else if (overwrite)
                            m_methods[key] = MakeMethodCallFunc(getter, inst);
                    }

                    if (typeof(Delegate).IsAssignableFrom(prop.PropertyType))
                    {
                        var method = (prop.GetValue(inst, null) as Delegate).Method;
                        var key = MakePropertyKey(prop, method);

                        if (!m_methods.ContainsKey(key))
                            m_methods.Add(key, MakePropertyCallFunc(prop, method, inst));
                        else if (overwrite)
                            m_methods[key] = MakePropertyCallFunc(prop, method, inst);
                    }
                }
            }
        }

        private string MakeMethodKey(MethodBase method)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(method.Name);

            IEnumerable<ParameterInfo> parameters = method.GetParameters();

            if (parameters.Count() > 0 && parameters.First().Name == "self")
                parameters = parameters.Skip(1);

            foreach (var param in parameters)
            {
                sb.Append("_");
                sb.Append(param.ParameterType.FullName);
            }

            return sb.ToString().ToLower();
        }

        private Func<object[], object> MakeMethodCallFunc(MethodInfo method, object obj)
        {
            var inst = Expression.Constant(obj);
            var param = Expression.Parameter(typeof(object[]), "param");

            IEnumerable<ParameterInfo> parameters = method.GetParameters();
            ParameterInfo first = parameters.FirstOrDefault();

            IEnumerable<Expression> callArgs = null;

            if (first != null && first.Name.ToLower() == "self")
            {
                List<Expression> tempArgs = new List<Expression>();
                tempArgs.Add(Expression.Convert(this.MakeGetProxyCall(), first.ParameterType));

                tempArgs.AddRange(parameters.Skip(1).Select((info, i) =>
                    Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), info.ParameterType)).Cast<Expression>());

                callArgs = tempArgs;
            }
            else
                callArgs = parameters.Select((info, i) =>
                    Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), info.ParameterType)).ToArray();

            var call = Expression.Call(
                inst,
                method,
                callArgs);

            if (method.ReturnType == typeof(void))
            {
                var action = Expression.Lambda<Action<object[]>>(call, param).Compile();
                return args =>
                {
                    action(args);
                    return null;
                };
            }
            else
                return Expression.Lambda<Func<object[], object>>(Expression.Convert(call, typeof(object)), param).Compile();
        }

        private string MakePropertyKey(PropertyInfo prop, MethodInfo method)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(prop.Name);

            IEnumerable<ParameterInfo> parameters = method.GetParameters();

            if (parameters.Count() > 0 && parameters.First().Name == "self")
                parameters = parameters.Skip(1);

            foreach (var param in parameters)
            {
                sb.Append("_");
                sb.Append(param.ParameterType.FullName);
            }

            return sb.ToString().ToLower();
        }

        private Func<object[], object> MakePropertyCallFunc(PropertyInfo propInfo, MethodInfo method, object obj)
        {
            var inst = Expression.Constant(obj);
            var param = Expression.Parameter(typeof(object[]), "param");
            var prop = Expression.Property(inst, propInfo);

            IEnumerable<ParameterInfo> parameters = method.GetParameters();
            ParameterInfo first = parameters.FirstOrDefault();

            IEnumerable<Expression> callArgs = null;

            if (first != null && first.Name.ToLower() == "self")
            {
                List<Expression> tempArgs = new List<Expression>();
                tempArgs.Add(Expression.Convert(this.MakeGetProxyCall(), first.ParameterType));

                tempArgs.AddRange(parameters.Skip(1).Select((info, i) =>
                    Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), info.ParameterType)).Cast<Expression>());

                callArgs = tempArgs;
            }
            else
                callArgs = parameters.Select((info, i) =>
                    Expression.Convert(Expression.ArrayIndex(param, Expression.Constant(i)), info.ParameterType)).ToArray();

            var invoke = Expression.Invoke(
                prop,
                callArgs);

            if (method.ReturnType == typeof(void))
            {
                var action = Expression.Lambda<Action<object[]>>(invoke, param).Compile();
                return args =>
                {
                    action(args);
                    return null;
                };
            }
            else
                return Expression.Lambda<Func<object[], object>>(Expression.Convert(invoke, typeof(object)), param).Compile();
        }

        private Expression MakeGetProxyCall()
        {
            return Expression.Call(Expression.Constant(this), typeof(Dynamic).GetMethod("GetTransparentProxy"));
        }

        private string MakeKey(string methodName, IEnumerable<Type> args)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(methodName);

            foreach (var type in args)
            {
                sb.Append("_");
                sb.Append(type.FullName);
            }

            return sb.ToString().ToLower();
        }

        #region RealProxy Members

        const string METHODMISSING = "methodmissing_system.string_system.object[]";

        public override IMessage Invoke(IMessage msg)
        {
            if (msg is IMethodCallMessage)
            {
                var call = msg as IMethodCallMessage;
                var methodInfo = call.MethodBase as MethodInfo;

                if (methodInfo.DeclaringType == typeof(IDynamicOps))
                {
                    if (methodInfo.Name == "MixIn")
                    {
                        this.MixIn(call.Args[0], (bool)call.Args[1]);
                        return new ReturnMessage(null, null, 0, null, call);
                    }
                    else if (methodInfo.Name == "Send")
                    {
                        var methodName = call.Args.First().ToString();
                        var args = call.Args.Last() as object[];
                        var key = MakeKey(methodName, args.Select(arg => arg.GetType()));

                        if (m_methods.ContainsKey(key))
                            return new ReturnMessage(m_methods[key](args), null, 0, null, call);
                        else if (m_methods.ContainsKey(METHODMISSING))
                            return new ReturnMessage(m_methods[METHODMISSING](new object[] { methodName, args }), null, 0, null, call);
                    }
                    else if (methodInfo.Name == "Define")
                    {
                        var methodName = call.Args.First().ToString();
                        var methodDelegate = call.Args.Last() as Delegate;
                        var info = methodDelegate.Method;
                        var pTypes = info.GetParameters().Select(p => p.ParameterType);
                        var key = MakeKey(methodName, pTypes);

                        var args = Expression.Parameter(typeof(object[]), "args") ;
                        var parameters = pTypes.Select((pt, i) => Expression.Convert(Expression.ArrayIndex(args, Expression.Constant(i)), pt)).ToArray();
                        var body = Expression.Invoke(
                            Expression.Constant(methodDelegate),
                            parameters);
                        var func = Expression.Lambda<Func<object[], object>>(body, args).Compile();

                        if (m_methods.ContainsKey(key))
                            m_methods[key] = func;
                        else
                            m_methods.Add(key, func);

                        return new ReturnMessage(null, null, 0, null, call);
                    }
                }
                else
                {
                    var key = MakeMethodKey(methodInfo);

                    if (m_methods.ContainsKey(key))
                        return new ReturnMessage(m_methods[key](call.Args), null, 0, null, call);
                    else if (m_methods.ContainsKey(METHODMISSING))
                        return new ReturnMessage(m_methods[METHODMISSING](new object[] { call.MethodName, call.Args }), null, 0, null, call);
                }
            }

            throw new NotImplementedException();
        }

        #endregion

        #region IRemotingTypeInfo Members

        public bool CanCastTo(Type fromType, object o)
        {
            return true;
        }

        public string TypeName { get; set; }

        #endregion

        class Reference
        {
            public object Value { get; set; }
        }
    }
}
