using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace AspNetCore.FileLog
{
    internal class FastExpressions
    {
        protected FastExpressions(object @object)
        {
            this.CurrentObject = @object;
            this.rootType = @object.GetType();
            this.ActionType = Expression.GetActionType(new Type[]
            {
                this.rootType
            });
        }
        
        private Func<object, string, bool, object, object> GenerateExpressions()
        {
            ParameterExpression objectParameter = Expression.Parameter(FastExpressions.ObjectType, "@object");
            ParameterExpression nameParameter = Expression.Parameter(typeof(string), "name");
            ParameterExpression isAssignParameter = Expression.Parameter(typeof(bool), "isAssign");
            ParameterExpression valueParameter = Expression.Parameter(FastExpressions.ObjectType, "value");
            ParameterExpression currentVariable = Expression.Variable(this.rootType, "current");
            ParameterExpression instanceVariable = Expression.Variable(FastExpressions.ObjectType, "_");
            MethodCallExpression messageExpression = Expression.Call(FastExpressions.ConcatMethod, Expression.Constant("Cannot found the property or field '"), nameParameter, Expression.Constant("' of '" + this.rootType.FullName + "'"));
            NewExpression exception = Expression.New(FastExpressions.KeyNotFoundExceptionConstructor, new Expression[]
            {
                messageExpression
            });
            this.GenerateSwitchs(currentVariable, instanceVariable, valueParameter);
            SwitchExpression getSwitch = Expression.Switch(nameParameter, Expression.Throw(exception), (from x in this.switchCases
                                                                                                        where x.IsGetter
                                                                                                        select x.SwitchCase).ToArray<SwitchCase>());
            SwitchExpression setSwitch = Expression.Switch(nameParameter, Expression.Throw(exception), (from x in this.switchCases
                                                                                                        where !x.IsGetter
                                                                                                        select x.SwitchCase).ToArray<SwitchCase>());
            ConditionalExpression assign = Expression.IfThen(Expression.NotEqual(valueParameter, Expression.Constant(null)), Expression.Assign(instanceVariable, valueParameter));
            BinaryExpression realVariable = Expression.Assign(currentVariable, Expression.TypeAs(objectParameter, this.rootType));
            List<Expression> blockExpressions = new List<Expression>
            {
                instanceVariable,
                assign,
                realVariable
            };
            ConditionalExpression _if = Expression.IfThenElse(isAssignParameter, setSwitch, getSwitch);
            ParameterExpression _ex = Expression.Parameter(typeof(Exception), "ex");
            MethodCallExpression _messageExpression = Expression.Call(FastExpressions.ConcatMethod, Expression.Constant(" Occur error when Get or Set the property or field '"), nameParameter, Expression.Constant("' of '" + this.rootType.FullName + "' "));
            NewExpression _exception = Expression.New(FastExpressions.ExceptionConstructor, new Expression[]
            {
                _messageExpression,
                _ex
            });
            TryExpression _try = Expression.TryCatch(Expression.Block(new Expression[]
            {
                _if
            }), new CatchBlock[]
            {
                Expression.Catch(_ex, Expression.Throw(_exception))
            });
            blockExpressions.Add(_try);
            blockExpressions.Add(instanceVariable);
            BlockExpression block = Expression.Block(new ParameterExpression[]
            {
                instanceVariable,
                currentVariable
            }, blockExpressions);
            Expression<Func<object, string, bool, object, object>> lambda = Expression.Lambda<Func<object, string, bool, object, object>>(block, new ParameterExpression[]
            {
                objectParameter,
                nameParameter,
                isAssignParameter,
                valueParameter
            });
            return lambda.Compile();
        }
        
        private void GenerateSwitchs(Expression currentVariable, Expression instanceVariable, Expression valueParameter)
        {
            ValueTuple<PropertyInfo[], FieldInfo[]> pf = this.GetPropertiesAndFields();
            foreach (PropertyInfo p in pf.Item1)
            {
                Expression _current = currentVariable;
                bool flag = p.DeclaringType != this.rootType;
                if (flag)
                {
                    _current = Expression.TypeAs(currentVariable, p.DeclaringType);
                }
                this.CreateSwitchForProperty(p.DeclaringType, p, _current, instanceVariable, valueParameter);
            }
            foreach (FieldInfo fi in pf.Item2)
            {
                bool flag2 = FastExpressions.BackingFieldRegex.IsMatch(fi.Name);
                if (!flag2)
                {
                    Expression _current2 = currentVariable;
                    bool flag3 = fi.DeclaringType != this.rootType;
                    if (flag3)
                    {
                        _current2 = Expression.TypeAs(currentVariable, fi.DeclaringType);
                    }
                    this.CreateSwitchForField(fi.DeclaringType, fi, _current2, instanceVariable, valueParameter);
                }
            }
        }
        
        //[return: TupleElementNames(new string[]
        //{
        //    "Properties",
        //    "Fields"
        //})]
        //[return: ValueTuple<PropertyInfo[], FieldInfo[]>("","")]
        private ValueTuple<PropertyInfo[], FieldInfo[]> GetPropertiesAndFields()
        {
            List<PropertyInfo> pis = this.rootType.GetRuntimeProperties().ToList<PropertyInfo>();
            List<FieldInfo> fis = this.rootType.GetRuntimeFields().ToList<FieldInfo>();
            Type _type = this.rootType.BaseType;
            while (_type != null)
            {
                IEnumerable<PropertyInfo> _pis = _type.GetRuntimeProperties();
                using (IEnumerator<PropertyInfo> enumerator = _pis.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        PropertyInfo _p = enumerator.Current;
                        bool flag = pis.Any((PropertyInfo x) => !x.Name.Equals(_p.Name));
                        if (flag)
                        {
                            pis.Add(_p);
                        }
                    }
                }
                IEnumerable<FieldInfo> _fis = _type.GetRuntimeFields();
                using (IEnumerator<FieldInfo> enumerator2 = _fis.GetEnumerator())
                {
                    while (enumerator2.MoveNext())
                    {
                        FieldInfo _f = enumerator2.Current;
                        bool flag2 = fis.Any((FieldInfo x) => !x.Name.Equals(_f.Name));
                        if (flag2)
                        {
                            fis.Add(_f);
                        }
                    }
                }
                _type = _type.BaseType;
            }
            this.Properties = pis.ToArray();
            this.Fields = fis.ToArray();
            return new ValueTuple<PropertyInfo[], FieldInfo[]>(this.Properties, this.Fields);
        }
        
        private void CreateSwitchForProperty(Type type, PropertyInfo pi, Expression currentVariable, Expression instanceVariable, Expression valueParameter)
        {
            FieldInfo field = null;
            bool canWrite = pi.CanWrite;
            if (canWrite)
            {
                MemberExpression member = Expression.Property(pi.SetMethod.IsStatic ? null : currentVariable, pi);
                BinaryExpression assign = Expression.Assign(member, Expression.Convert(valueParameter, pi.PropertyType));
                this.AddSwitch(Expression.Block(assign, Expression.Empty()), pi.Name, false);
            }
            else
            {
                field = this.Fields.FirstOrDefault((FieldInfo fi) => Regex.IsMatch(fi.Name, "^<" + pi.Name + ">.+Field$"));
                bool flag = field != null;
                if (flag)
                {
                    bool isInitOnly = field.IsInitOnly;
                    if (isInitOnly)
                    {
                        MethodCallExpression call = Expression.Call(Expression.Constant(field), FastExpressions.FieldSetValueMethod, currentVariable, valueParameter);
                        this.AddSwitch(call, pi.Name, false);
                    }
                    else
                    {
                        MemberExpression member2 = Expression.Field(field.IsStatic ? null : currentVariable, field);
                        BinaryExpression assign2 = Expression.Assign(member2, Expression.Convert(valueParameter, pi.PropertyType));
                        this.AddSwitch(Expression.Block(assign2, Expression.Empty()), pi.Name, false);
                    }
                }
            }
            bool canRead = pi.CanRead;
            if (canRead)
            {
                BinaryExpression assign3 = Expression.Assign(instanceVariable, Expression.Convert(Expression.Property(pi.GetMethod.IsStatic ? null : currentVariable, pi), FastExpressions.ObjectType));
                this.AddSwitch(Expression.Block(assign3, Expression.Empty()), pi.Name, true);
            }
            else
            {
                bool flag2 = field != null;
                if (flag2)
                {
                    BinaryExpression assign4 = Expression.Assign(instanceVariable, Expression.Convert(Expression.Field(field.IsStatic ? null : currentVariable, field), FastExpressions.ObjectType));
                    this.AddSwitch(Expression.Block(assign4, Expression.Empty()), pi.Name, true);
                }
            }
        }
        
        private void CreateSwitchForField(Type type, FieldInfo field, Expression currentVariable, Expression instanceVariable, Expression valueParameter)
        {
            bool isLiteral = field.IsLiteral;
            if (!isLiteral)
            {
                bool isInitOnly = field.IsInitOnly;
                if (isInitOnly)
                {
                    MethodCallExpression call = Expression.Call(Expression.Constant(field), FastExpressions.FieldSetValueMethod, currentVariable, valueParameter);
                    this.AddSwitch(call, field.Name, false);
                }
                else
                {
                    MemberExpression member = Expression.Field(field.IsStatic ? null : currentVariable, field);
                    BinaryExpression assign = Expression.Assign(member, Expression.Convert(valueParameter, field.FieldType));
                    this.AddSwitch(Expression.Block(assign, Expression.Empty()), field.Name, false);
                }
                BinaryExpression _assign = Expression.Assign(instanceVariable, Expression.Convert(Expression.Field(field.IsStatic ? null : currentVariable, field), FastExpressions.ObjectType));
                this.AddSwitch(Expression.Block(_assign, Expression.Empty()), field.Name, true);
            }
        }
        
        private void AddSwitch(Expression expr, string name, bool isGetter)
        {
            bool flag = !this.switchCases.Any((FastExpressions._SwitchCase x) => x.IsGetter && x.Name == name);
            if (flag)
            {
                this.switchCases.Add(new FastExpressions._SwitchCase
                {
                    SwitchCase = Expression.SwitchCase(expr, new Expression[]
                    {
                        Expression.Constant(name)
                    }),
                    IsGetter = isGetter,
                    Name = name
                });
            }
        }
        
        public static Func<object, string, bool, object, object> CreateDelegate(object @object)
        {
            bool flag = @object == null;
            Func<object, string, bool, object, object> result;
            if (flag)
            {
                result = null;
            }
            else
            {
                Type type = @object.GetType();
                result = FastExpressions.GetterSetters.GetOrAdd(type, delegate (Type _func)
                {
                    FastExpressions exp = new FastExpressions(@object);
                    return exp.GenerateExpressions();
                });
            }
            return result;
        }
        
        private static readonly Regex BackingFieldRegex = new Regex("^<([^><]+)>.+Field$", RegexOptions.Compiled);
        
        private static MethodInfo FieldSetValueMethod = typeof(FieldInfo).GetMethod("SetValue", new Type[]
        {
            typeof(object),
            typeof(object)
        });
        
        private static ConcurrentDictionary<Type, Func<object, string, bool, object, object>> GetterSetters = new ConcurrentDictionary<Type, Func<object, string, bool, object, object>>();
        
        private static readonly ConstructorInfo KeyNotFoundExceptionConstructor = typeof(KeyNotFoundException).GetConstructor(new Type[]
        {
            typeof(string)
        });
        
        private static readonly MethodInfo ConcatMethod = typeof(string).GetMethod("Concat", new Type[]
        {
            typeof(string),
            typeof(string),
            typeof(string)
        });
        
        private static readonly ConstructorInfo ExceptionConstructor = typeof(Exception).GetConstructor(new Type[]
        {
            typeof(string),
            typeof(Exception)
        });
        
        private static readonly object _state = new object();
        
        private static readonly Type ObjectType = typeof(object);
        
        private Type rootType;

        private Type ActionType;
        
        private object CurrentObject;
        
        private PropertyInfo[] Properties;
        
        private FieldInfo[] Fields;
        
        private List<FastExpressions._SwitchCase> switchCases = new List<FastExpressions._SwitchCase>();
        
        private class _SwitchCase
        {
            public SwitchCase SwitchCase { get; set; }
            
            public bool IsGetter { get; set; }
            
            public string Name { get; set; }
        }
    }
}
