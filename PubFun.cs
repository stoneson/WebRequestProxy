using HCenter;
using HCenter.CommonUtils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace WebRequestProxy
{
    public static class PubFun
    {
        #region GetTypeByName
        public static Type GetTypeByName(string typeName)
        {
            var type = GetTypeByString(typeName);
            if (type != null)
            {
                return type;
            }
            //------------------------------------------------------------------------
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
            int assemblyArrayLength = assemblyArray.Length;
            for (int i = 0; i < assemblyArrayLength; ++i)
            {
                type = assemblyArray[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            //------------------------------------------------------------------------
            //for (int i = 0; (i < assemblyArrayLength); ++i)
            //{
            //    Type[] typeArray = assemblyArray[i].GetTypes();
            //    int typeArrayLength = typeArray.Length;
            //    for (int j = 0; j < typeArrayLength; j++)
            //    {
            //        try
            //        {
            //            var tye = typeArray[j];
            //            if (tye != null && tye.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
            //            {
            //                return tye;
            //            }
            //        }
            //        catch { }
            //    }
            //}
            return type;
        }
        private static Type GetTypeByString(string type)
        {
            switch (type.ToLower())
            {
                case "bool":
                    return Type.GetType("System.Boolean", false, true);
                case "byte":
                    return Type.GetType("System.Byte", false, true);
                case "sbyte":
                    return Type.GetType("System.SByte", false, true);
                case "char":
                    return Type.GetType("System.Char", false, true);
                case "decimal":
                    return Type.GetType("System.Decimal", false, true);
                case "double":
                    return Type.GetType("System.Double", false, true);
                case "single":
                case "float":
                    return Type.GetType("System.Single", false, true);
                case "int32":
                case "int":
                    return Type.GetType("System.Int32", false, true);
                case "uint32":
                case "uint":
                    return Type.GetType("System.UInt32", false, true);
                case "int64":
                case "long":
                    return Type.GetType("System.Int64", false, true);
                case "uint64":
                case "ulong":
                    return Type.GetType("System.UInt64", false, true);
                case "object":
                    return Type.GetType("System.Object", false, true);
                case "int16":
                case "short":
                    return Type.GetType("System.Int16", false, true);
                case "uint16":
                case "ushort":
                    return Type.GetType("System.UInt16", false, true);
                case "string":
                    return Type.GetType("System.String", false, true);
                case "date":
                case "datetime":
                    return Type.GetType("System.DateTime", false, true);
                case "guid":
                    return Type.GetType("System.Guid", false, true);
                default:
                    return Type.GetType(type, false, true);
            }
        }
        #endregion

        #region Table2Entity
        #region internal
        internal delegate T Load<T>(DataRow DrRecord);
        internal delegate object LoadType(DataRow DrRecord, Type type);
        private static readonly MethodInfo mGetValueMet = typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(int) });
        private static readonly MethodInfo mIsDBNullMet = typeof(DataRow).GetMethod("IsNull", new Type[] { typeof(int) });
        private static Dictionary<Type, Delegate> mRowMapMets = new Dictionary<Type, Delegate>();
        private static Dictionary<Type, MethodInfo> mConvertMets = new Dictionary<Type, MethodInfo>()
       {
           {typeof(int),typeof(FuncStr).GetMethod("NullToInt",new Type[]{typeof(object)})},
           {typeof(Int16),typeof(FuncStr).GetMethod("NullToInt",new Type[]{typeof(object)})},
           {typeof(Int64),typeof(Convert).GetMethod("ToInt64",new Type[]{typeof(object)})},
           {typeof(DateTime),typeof(Convert).GetMethod("ToDateTime",new Type[]{typeof(object)})},
           //  {typeof(DateTime?),typeof(Convert).GetMethod("ToDateTime",new Type[]{typeof(object)})},
           {typeof(decimal),typeof(FuncStr).GetMethod("NullToDecimal",new Type[]{typeof(object)})},
           {typeof(double),typeof(FuncStr).GetMethod("NullToDouble",new Type[]{typeof(object)})},
           {typeof(Boolean),typeof(Convert).GetMethod("ToBoolean",new Type[]{typeof(object)})},
           {typeof(char),typeof(Convert).GetMethod("ToChar",new Type[]{typeof(object)})},
           {typeof(string),typeof(FuncStr).GetMethod("NullToStr",new Type[]{typeof(object)})},
           {typeof(byte),typeof(Convert).GetMethod("ToByte",new Type[]{typeof(object)})},
           {typeof(Single),typeof(Convert).GetMethod("ToSingle",new Type[]{typeof(object)})}
       };
        internal static TEnum ToEnum<TEnum, TUnder>(object obj)
        {
            return (TEnum)Convert.ChangeType(obj, typeof(TUnder));
        }
        internal static TEnum StrToEnum<TEnum>(object value) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(value.NullToStr(), out TEnum enumStr))
            {
                return enumStr;
            }

            return default(TEnum);
        }
        internal static T ToIntCuInt<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(int));
        }

        internal static T ToIntCuDecimal<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(decimal));
        }
        internal static T ToIntCuDouble<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(double));
        }
        internal static T ToIntCuDateTime<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(DateTime));
        }

        internal static T ToGuid<T>(object obj)
        {
            return (T)Convert.ChangeType(obj, typeof(Guid));
        }
        internal static Load<T> GetDataRow2EntityFunc<T>(DataTable _tab) where T : new()
        {
            Load<T> _RowMap = null;
            if (_tab == null) return _RowMap;

            if (!mRowMapMets.ContainsKey(typeof(T)))
            {
                DynamicMethod _Method = new DynamicMethod("DyEntity_" + typeof(T).Name, typeof(T), new Type[] { typeof(DataRow) }, typeof(T), true);
                ILGenerator _Generator = _Method.GetILGenerator();
                LocalBuilder _Result = _Generator.DeclareLocal(typeof(T));
                _Generator.Emit(OpCodes.Newobj, typeof(T).GetConstructor(Type.EmptyTypes));
                _Generator.Emit(OpCodes.Stloc, _Result);
                for (int i = 0; i < _tab.Columns.Count; i++)
                {
                    PropertyInfo propertyInfo = typeof(T).GetProperties().ToList().Find(p => p.Name.ToLower().Equals(_tab.Columns[i].ColumnName.ToLower()));
                    System.Reflection.Emit.Label endIfLabel = _Generator.DefineLabel();
                    if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                    {
                        _Generator.Emit(OpCodes.Ldarg_0);
                        _Generator.Emit(OpCodes.Ldc_I4, i);
                        _Generator.Emit(OpCodes.Callvirt, mIsDBNullMet);
                        _Generator.Emit(OpCodes.Brtrue, endIfLabel);
                        _Generator.Emit(OpCodes.Ldloc, _Result);
                        _Generator.Emit(OpCodes.Ldarg_0);
                        _Generator.Emit(OpCodes.Ldc_I4, i);
                        _Generator.Emit(OpCodes.Callvirt, mGetValueMet);
                        if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string))
                            if (propertyInfo.PropertyType.IsEnum)
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToEnum", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType, Enum.GetUnderlyingType(propertyInfo.PropertyType)));
                            else if (propertyInfo.PropertyType == typeof(int?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuInt", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(decimal?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuDecimal", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(double?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuDouble", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(DateTime?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuDateTime", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(Guid))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToGuid", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else
                                _Generator.Emit(OpCodes.Call, mConvertMets[propertyInfo.PropertyType]);
                        else
                            _Generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
                        _Generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                        _Generator.MarkLabel(endIfLabel);
                    }
                }
                _Generator.Emit(OpCodes.Ldloc, _Result);
                _Generator.Emit(OpCodes.Ret);
                _RowMap = (Load<T>)_Method.CreateDelegate(typeof(Load<T>));
            }
            else
                _RowMap = (Load<T>)mRowMapMets[typeof(T)];

            return _RowMap;
        }
        internal static LoadType GetDataRow2EntityFunc(DataTable _tab, Type type)
        {
            LoadType _RowMap = null;
            if (_tab == null) return _RowMap;

            if (!mRowMapMets.ContainsKey(type))
            {
                DynamicMethod _Method = new DynamicMethod("DyEntity_" + type.Name, type, new Type[] { typeof(DataRow), typeof(Type) }, type, true);
                ILGenerator _Generator = _Method.GetILGenerator();
                LocalBuilder _Result = _Generator.DeclareLocal(type);
                _Generator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));
                _Generator.Emit(OpCodes.Stloc, _Result);
                for (int i = 0; i < _tab.Columns.Count; i++)
                {
                    PropertyInfo propertyInfo = type.GetProperties().ToList().Find(p => p.Name.ToLower().Equals(_tab.Columns[i].ColumnName.ToLower()));
                    System.Reflection.Emit.Label endIfLabel = _Generator.DefineLabel();
                    if (propertyInfo != null && propertyInfo.GetSetMethod() != null)
                    {
                        _Generator.Emit(OpCodes.Ldarg_0);
                        _Generator.Emit(OpCodes.Ldc_I4, i);
                        _Generator.Emit(OpCodes.Callvirt, mIsDBNullMet);
                        _Generator.Emit(OpCodes.Brtrue, endIfLabel);
                        _Generator.Emit(OpCodes.Ldloc, _Result);
                        _Generator.Emit(OpCodes.Ldarg_0);
                        _Generator.Emit(OpCodes.Ldc_I4, i);
                        _Generator.Emit(OpCodes.Callvirt, mGetValueMet);
                        if (propertyInfo.PropertyType.IsValueType || propertyInfo.PropertyType == typeof(string))
                            if (propertyInfo.PropertyType.IsEnum)
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToEnum", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType, Enum.GetUnderlyingType(propertyInfo.PropertyType)));
                            else if (propertyInfo.PropertyType == typeof(int?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuInt", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(decimal?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuDecimal", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(double?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuDouble", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(DateTime?))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToIntCuDateTime", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else if (propertyInfo.PropertyType == typeof(Guid))
                                _Generator.Emit(OpCodes.Call, typeof(FuncTable2Entity).GetMethod("ToGuid", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(propertyInfo.PropertyType));
                            else
                                _Generator.Emit(OpCodes.Call, mConvertMets[propertyInfo.PropertyType]);
                        else
                            _Generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
                        _Generator.Emit(OpCodes.Callvirt, propertyInfo.GetSetMethod());
                        _Generator.MarkLabel(endIfLabel);
                    }
                }
                _Generator.Emit(OpCodes.Ldloc, _Result);
                _Generator.Emit(OpCodes.Ret);
                _RowMap = (LoadType)_Method.CreateDelegate(typeof(LoadType));
            }
            else
                _RowMap = (LoadType)mRowMapMets[type];

            return _RowMap;
        }
        #endregion

        /// <summary>
        /// 通用类型转换 Convert.ChangeType
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static object ConvertType(this object obj, Type conversionType)
        {
            MethodInfo methodInfo = null;
            if (conversionType.IsValueType || conversionType == typeof(string))
                if (conversionType.IsEnum)
                    methodInfo = typeof(FuncTable2Entity).GetMethod("StrToEnum", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(conversionType);
                else if (conversionType == typeof(int?))
                    methodInfo = typeof(FuncTable2Entity).GetMethod("ToIntCuInt", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(conversionType);
                else if (conversionType == typeof(decimal?))
                    methodInfo = typeof(FuncTable2Entity).GetMethod("ToIntCuDecimal", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(conversionType);
                else if (conversionType == typeof(double?))
                    methodInfo = typeof(FuncTable2Entity).GetMethod("ToIntCuDouble", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(conversionType);
                else if (conversionType == typeof(DateTime?))
                    methodInfo = typeof(FuncTable2Entity).GetMethod("ToIntCuDateTime", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(conversionType);
                else if (conversionType == typeof(Guid))
                    methodInfo = typeof(FuncTable2Entity).GetMethod("ToGuid", BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(conversionType);
                else
                    methodInfo = mConvertMets[conversionType];
            return methodInfo?.Invoke(obj, null);
        }
        /// <summary>
        /// 获取列值
        /// </summary>
        /// <param name="_dr"></param>
        /// <param name="_colName">列名</param>
        /// <returns></returns>
        public static object GetColumnValue(this DataRow _dr, string _colName)
        {
            if (_dr == null || _colName.IsNullOrEmpty() || _dr.Table == null) return null;
            if (_dr.Table.Columns.Contains(_colName))
            {
                if (_dr[_colName].IsNullOrEmpty()) return null;
                return _dr[_colName];
            }
            return null;
        }
        /// <summary>
        /// 数据行复制
        /// </summary>
        /// <param name="_drFrom"></param>
        /// <param name="_drTo"></param>
        public static void CopyTo(this DataRow _drFrom, DataRow _drTo)
        {
            if (_drFrom == null || _drTo == null || _drFrom.Table == null || _drTo.Table == null) return;
            foreach (DataColumn col in _drTo.Table.Columns)
            {
                if (_drFrom.Table.Columns.Contains(col.ColumnName))
                    _drTo[col.ColumnName] = _drFrom.GetColumnValue(col.ColumnName);
            }
        }

        /// <summary>
        /// DataRow转换成Entity
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="_dr">DataRow</param>
        /// <returns></returns>
        public static T ToEntity<T>(this DataRow _dr) where T : new()
        {
            if (_dr == null || _dr.Table == null) return default(T);

            var _RowMap = GetDataRow2EntityFunc<T>(_dr.Table);
            if (_RowMap == null) return default(T);
            return _RowMap(_dr);
        }
        /// <summary>
        /// DataRow转换成Entity
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="_dr">DataRow</param>
        /// <returns></returns>
        public static object ToEntity(this DataRow _dr, Type type)
        {
            if (_dr == null || _dr.Table == null) return new object();

            var _RowMap = GetDataRow2EntityFunc(_dr.Table, type);
            if (_RowMap == null) return new object();
            return _RowMap(_dr, type);
        }
        /// <summary>
        /// DataTable转换成对象的List集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="_tab">DataTable</param>
        /// <returns></returns>
        public static List<T> ToList<T>(this DataTable _tab) where T : new()
        {
            List<T> _ResultList = new List<T>();
            if (_tab == null) return _ResultList;

            var _RowMap = GetDataRow2EntityFunc<T>(_tab);
            if (_RowMap == null) return _ResultList;
            foreach (DataRow info in _tab.Rows)
                _ResultList.Add(_RowMap(info));
            return _ResultList;
        }

        /// <summary>
        /// DataTable转换成对象的List集合
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="_tab">DataTable</param>
        /// <returns></returns>
        public static List<object> ToList(this DataTable _tab, Type type)
        {
            List<object> _ResultList = new List<object>();
            if (_tab == null) return _ResultList;

            var _RowMap = GetDataRow2EntityFunc(_tab, type);
            if (_RowMap == null) return _ResultList;
            foreach (DataRow info in _tab.Rows)
                _ResultList.Add(_RowMap(info, type));
            return _ResultList;
        }

        /// <summary>  
        /// DataTable转成Newtonsoft.Json.Linq.JArray   
        /// </summary>  
        /// <param name="jsonName"></param>  
        /// <param name="dt"></param>  
        /// <returns></returns>  
        public static dynamic ToJObject(this DataTable dt, string jsonName = "", bool isNameNullUseTableName = false)
        {
            var Json = new Newtonsoft.Json.Linq.JArray();
            if (dt == null) return Json;

            if (string.IsNullOrEmpty(jsonName) && isNameNullUseTableName)
                jsonName = dt.TableName;
            //---------------------------------------------------------------------------------------------------------
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Json.Add(ToJObject(dt.Rows[i], ""));//, fillerCol , params string[] fillerCol
                }
            }
            //---------------------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonName))
            {
                return Json;
            }
            else
            {
                var ret = new Newtonsoft.Json.Linq.JObject();
                ret[jsonName] = Json;// new Newtonsoft.Json.Linq.JValue(dr[j]);
                return ret;
            }
        }
        /// <summary>  
        /// DataRow转成 Newtonsoft.Json.Linq.JObject   
        /// </summary>  
        /// <param name="jsonName"></param>  
        /// <param name="dt"></param>  
        /// <returns></returns>  
        public static Newtonsoft.Json.Linq.JObject ToJObject(this DataRow dr, string jsonName = "")
        {
            var Json = new Newtonsoft.Json.Linq.JObject();
            if (dr == null) return Json;

            //var list = new List<string>(fillerCol);, params string[] fillerCol
            //---------------------------------------------------------------------------------------------------------
            for (int j = 0; j < dr.Table.Columns.Count; j++)
            {
                // if (list?.Exists(s => s.ToLower() == dr.Table.Columns[j].ColumnName.ToLower()) == true)
                //     continue;
                var obj = dr[j];
                if (obj.NullToStr().IsJson())
                {
                    obj = Newtonsoft.Json.JsonConvert.DeserializeObject(dr[j].NullToStr());
                    Json[dr.Table.Columns[j].ColumnName] = obj as Newtonsoft.Json.Linq.JToken;// new Newtonsoft.Json.Linq.JValue(obj);
                }
                else
                {
                    Json[dr.Table.Columns[j].ColumnName] = new Newtonsoft.Json.Linq.JValue(obj);
                }
            }
            //---------------------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonName))
            {
                return Json;
            }
            else
            {
                var ret = new Newtonsoft.Json.Linq.JObject();
                ret[jsonName] = Json;// new Newtonsoft.Json.Linq.JValue(dr[j]);
                return ret;
            }
        }
        //==============================================================================================================
        /// <summary>  
        /// DataTable转成dynamic
        /// </summary>  
        /// <param name="jsonName"></param>  
        /// <param name="dt"></param>  
        /// <returns></returns>  
        public static dynamic ToDynamic(this DataTable dt, string jsonName = "", bool isNameNullUseTableName = false)
        {
            List<ExpandoObject> _arrexpando = new List<ExpandoObject>();
            if (dt == null) return _arrexpando;

            if (string.IsNullOrEmpty(jsonName) && isNameNullUseTableName)
                jsonName = dt.TableName;
            //---------------------------------------------------------------------------------------------------------
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    _arrexpando.Add(ToDynamic(dt.Rows[i], ""));
                }
            }
            //---------------------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonName))
            {
                return _arrexpando;
            }
            else
            {
                dynamic ret = new ExpandoObject();
                IDictionary<string, object> dyret = ret;
                dyret[jsonName] = _arrexpando;
                return ret;
            }
        }
        /// <summary>  
        /// DataRow转成 dynamic
        /// </summary>  
        /// <param name="jsonName"></param>  
        /// <param name="dr"></param>  
        /// <returns></returns>  
        public static dynamic ToDynamic(this DataRow dr, string jsonName = "")
        {
            if (dr == null)
                return new { };

            dynamic _expando = new ExpandoObject();
            IDictionary<string, object> dy = _expando;
            //---------------------------------------------------------------------------------------------------------
            for (int j = 0; j < dr.Table.Columns.Count; j++)
            {
                dy[dr.Table.Columns[j].ColumnName] = dr[j];
            }
            //---------------------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(jsonName))
            {
                return dy;
            }
            else
            {
                dynamic ret = new ExpandoObject();
                IDictionary<string, object> dyret = ret;
                dyret[jsonName] = _expando;
                return ret;
            }
        }
        /// <summary>
        /// object 对象转dynamic
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static dynamic ToDynamic(this object obj)
        {
            if (obj == null || obj == DBNull.Value)
                return new { };
            if (obj is System.Dynamic.ExpandoObject)
                return obj;

            dynamic _expando = new ExpandoObject();
            IDictionary<string, object> dy = _expando;
            //---------------------------------------------------------------------------------------------------------
            if (obj is Newtonsoft.Json.Linq.JObject _jObject)//JSON Object
            {
                return ToDynamic(_jObject);
            }
            else if (obj is Newtonsoft.Json.Linq.JArray _ajObject)//JSON JArray
            {
                return ToDynamic(_ajObject);
            }
            else if (obj is System.Collections.IDictionary _dicObject)//IDictionary
            {
                foreach (object key in _dicObject.Keys)
                {
                    var val = _dicObject[key];
                    var dyPropName = key.ToString();
                    dy[dyPropName] = val;
                }
            }
            else if (obj is DataRow _drObject)//DataRow
            {
                return ToDynamic(_drObject);
            }
            else if (obj is DataTable _dtObject)//DataTable
            {
                return ToDynamic(_dtObject);
            }
            else//实体
            {
                return obj;
                //var properties = obj.GetType().GetProperties().ToList();
                //if (properties?.Count > 0)
                //{
                //    properties.ForEach(p =>
                //    {
                //        var val = p.GetValue(obj);
                //        dy[p.Name] = val;
                //    });
                //}
                //var fields = obj.GetType().GetFields().ToList();
                //if (fields?.Count > 0)
                //{
                //    fields.ForEach(f =>
                //    {
                //        var val = f.GetValue(obj);
                //        dy[f.Name] = val;
                //    });
                //}
            }

            return _expando;
        }
        /// <summary>
        /// Newtonsoft.Json.Linq.JObject对象转dynamic
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static dynamic ToDynamic(this Newtonsoft.Json.Linq.JObject obj)
        {
            if (obj == null)
                return new { };

            dynamic _expando = new ExpandoObject();
            IDictionary<string, object> dy = _expando;
            foreach (var item in obj)
            {
                var val = item.Value;
                if (val != null)
                {
                    if (val is Newtonsoft.Json.Linq.JObject _sjObject)
                    {
                        dy[item.Key] = ToDynamic(_sjObject);
                    }
                    else if (val is Newtonsoft.Json.Linq.JArray _ajObject)
                    {
                        dy[item.Key] = ToDynamic(_ajObject);
                    }
                    else
                    {
                        dy[item.Key] = (val as Newtonsoft.Json.Linq.JValue).Value;
                    }
                    continue;
                }
                dy[item.Key] = null;
            }
            return _expando;
        }
        /// <summary>
        /// Newtonsoft.Json.Linq.JArray 对象转dynamic
        /// </summary>
        /// <param name="jarray"></param>
        /// <returns></returns>
        public static dynamic ToDynamic(this Newtonsoft.Json.Linq.JArray jarray)
        {
            if (jarray == null)
                return new List<dynamic>();

            var _expando = new List<dynamic>();
            foreach (var item in jarray)
            {
                if (item.HasValues)
                {
                    var val = item.Value<object>();
                    if (val != null)
                    {
                        if (val is Newtonsoft.Json.Linq.JObject _sjObject)
                        {
                            _expando.Add(ToDynamic(_sjObject));
                        }
                        else if (val is Newtonsoft.Json.Linq.JArray _ajObject)
                        {
                            _expando.Add(ToDynamic(_ajObject));
                        }
                        else
                        {
                            _expando.Add(val);
                        }
                        continue;
                    }
                }
            }
            return _expando;
        }
        #region IsJson
        public static bool IsJson(this string json)
        {
            int errIndex;
            return IsJson(json, out errIndex);
        }
        public static bool IsJson(this string json, out int errIndex)
        {
            errIndex = 0;
            json = json.Trim();
            if (string.IsNullOrEmpty(json) || json.Length < 2 ||
                ((json[0] != '{' && json[json.Length - 1] != '}') && (json[0] != '[' && json[json.Length - 1] != ']')))
            {
                return false;
            }
            CharState cs = new CharState();
            char c;
            for (int i = 0; i < json.Length; i++)
            {
                c = json[i];
                if (SetCharState(c, ref cs) && cs.childrenStart)//设置关键符号状态。
                {
                    string item = json.Substring(i);
                    int err;
                    int length = GetValueLength(item, true, out err);
                    cs.childrenStart = false;
                    if (err > 0)
                    {
                        errIndex = i + err;
                        return false;
                    }
                    i = i + length - 1;
                }
                if (cs.isError)
                {
                    errIndex = i;
                    return false;
                }
            }

            return !cs.arrayStart && !cs.jsonStart; //只要不是正常关闭，则失败
        }
        /// <summary>
        /// 获取值的长度（当Json值嵌套以"{"或"["开头时）
        /// </summary>
        private static int GetValueLength(string json, bool breakOnErr, out int errIndex)
        {
            errIndex = 0;
            int len = json.Length - 1;
            if (!string.IsNullOrEmpty(json))
            {
                CharState cs = new CharState();
                char c;
                for (int i = 0; i < json.Length; i++)
                {
                    c = json[i];
                    if (!SetCharState(c, ref cs))//设置关键符号状态。
                    {
                        if (!cs.jsonStart && !cs.arrayStart)//json结束，又不是数组，则退出。
                        {
                            break;
                        }
                    }
                    else if (cs.childrenStart)//正常字符，值状态下。
                    {
                        int length = GetValueLength(json.Substring(i), breakOnErr, out errIndex);//递归子值，返回一个长度。。。
                        cs.childrenStart = false;
                        cs.valueStart = 0;
                        //cs.state = 0;
                        i = i + length - 1;
                    }
                    if (breakOnErr && cs.isError)
                    {
                        errIndex = i;
                        return i;
                    }
                    if (!cs.jsonStart && !cs.arrayStart)//记录当前结束位置。
                    {
                        len = i + 1;//长度比索引+1
                        break;
                    }
                }
            }
            return len;
        }
        /// <summary>
        /// 字符状态
        /// </summary>
        private class CharState
        {
            internal bool jsonStart = false;//以 "{"开始了...
            internal bool setDicValue = false;// 可以设置字典值了。
            internal bool escapeChar = false;//以"\"转义符号开始了
            /// <summary>
            /// 数组开始【仅第一开头才算】，值嵌套的以【childrenStart】来标识。
            /// </summary>
            internal bool arrayStart = false;//以"[" 符号开始了
            internal bool childrenStart = false;//子级嵌套开始了。
            /// <summary>
            /// 【-1 未初始化】【0 取名称中】；【1 取值中】
            /// </summary>
            internal int state = -1;

            /// <summary>
            /// 【-2 已结束】【-1 未初始化】【0 未开始】【1 无引号开始】【2 单引号开始】【3 双引号开始】
            /// </summary>
            internal int keyStart = -1;
            /// <summary>
            /// 【-2 已结束】【-1 未初始化】【0 未开始】【1 无引号开始】【2 单引号开始】【3 双引号开始】
            /// </summary>
            internal int valueStart = -1;

            internal bool isError = false;//是否语法错误。

            internal void CheckIsError(char c)//只当成一级处理（因为GetLength会递归到每一个子项处理）
            {
                switch (c)
                {
                    case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                        isError = jsonStart && state == 0;//重复开始错误 同时不是值处理。
                        break;
                    case '}':
                        isError = !jsonStart || (keyStart > 0 && state == 0);//重复结束错误 或者 提前结束。
                        break;
                    case '[':
                        isError = arrayStart && state == 0;//重复开始错误
                        break;
                    case ']':
                        isError = !arrayStart || (state == 1 && valueStart == 0);//重复开始错误[{},]1,0  正常：[111,222] 1,1 [111,"22"] 1,-2 
                        break;
                    case '"':
                        isError = !jsonStart && !arrayStart;//未开始Json，同时也未开始数组。
                        break;
                    case '\'':
                        isError = !jsonStart && !arrayStart;//未开始Json
                        break;
                    case ':':
                        isError = (!jsonStart && !arrayStart) || (jsonStart && keyStart < 2 && valueStart < 2 && state == 1);//未开始Json 同时 只能处理在取值之前。
                        break;
                    case ',':
                        isError = (!jsonStart && !arrayStart)
                            || (!jsonStart && arrayStart && state == -1) //[,111]
                            || (jsonStart && keyStart < 2 && valueStart < 2 && state == 0);//未开始Json 同时 只能处理在取值之后。
                        break;
                    default: //值开头。。
                        isError = (!jsonStart && !arrayStart) || (keyStart == 0 && valueStart == 0 && state == 0);//
                        if (!isError && keyStart < 2)
                        {
                            if ((jsonStart && !arrayStart) && state != 1)
                            {
                                //不是引号开头的，只允许字母 {aaa:1}
                                isError = c < 65 || (c > 90 && c < 97) || c > 122;
                            }
                            else if (!jsonStart && arrayStart && valueStart < 2)//
                            {
                                //不是引号开头的，只允许数字[1]
                                isError = c < 48 || c > 57;

                            }
                        }
                        break;
                }
                //if (isError)
                //{

                //}
            }
        }
        /// <summary>
        /// 设置字符状态(返回true则为关键词，返回false则当为普通字符处理）
        /// </summary>
        private static bool SetCharState(char c, ref CharState cs)
        {
            switch (c)
            {
                case '{'://[{ "[{A}]":[{"[{B}]":3,"m":"C"}]}]
                    #region 大括号
                    if (cs.keyStart <= 0 && cs.valueStart <= 0)
                    {
                        cs.CheckIsError(c);
                        if (cs.jsonStart && cs.state == 1)
                        {
                            cs.valueStart = 0;
                            cs.childrenStart = true;
                        }
                        else
                        {
                            cs.state = 0;
                        }
                        cs.jsonStart = true;//开始。
                        return true;
                    }
                    #endregion
                    break;
                case '}':
                    #region 大括号结束
                    if (cs.keyStart <= 0 && cs.valueStart < 2)
                    {
                        cs.CheckIsError(c);
                        if (cs.jsonStart)
                        {
                            cs.jsonStart = false;//正常结束。
                            cs.valueStart = -1;
                            cs.state = 0;
                            cs.setDicValue = true;
                        }
                        return true;
                    }
                    // cs.isError = !cs.jsonStart && cs.state == 0;
                    #endregion
                    break;
                case '[':
                    #region 中括号开始
                    if (!cs.jsonStart)
                    {
                        cs.CheckIsError(c);
                        cs.arrayStart = true;
                        return true;
                    }
                    else if (cs.jsonStart && cs.state == 1 && cs.valueStart < 2)
                    {
                        cs.CheckIsError(c);
                        //cs.valueStart = 1;
                        cs.childrenStart = true;
                        return true;
                    }
                    #endregion
                    break;
                case ']':
                    #region 中括号结束
                    if (!cs.jsonStart && (cs.keyStart <= 0 && cs.valueStart <= 0) || (cs.keyStart == -1 && cs.valueStart == 1))
                    {
                        cs.CheckIsError(c);
                        if (cs.arrayStart)// && !cs.childrenStart
                        {
                            cs.arrayStart = false;
                        }
                        return true;
                    }
                    #endregion
                    break;
                case '"':
                case '\'':
                    cs.CheckIsError(c);
                    #region 引号
                    if (cs.jsonStart || cs.arrayStart)
                    {
                        if (!cs.jsonStart && cs.arrayStart)
                        {
                            cs.state = 1;//如果是数组，只有取值，没有Key，所以直接跳过0
                        }
                        if (cs.state == 0)//key阶段
                        {
                            cs.keyStart = (cs.keyStart <= 0 ? (c == '"' ? 3 : 2) : -2);
                            return true;
                        }
                        else if (cs.state == 1)//值阶段
                        {
                            if (cs.valueStart <= 0)
                            {
                                cs.valueStart = (c == '"' ? 3 : 2);
                                return true;
                            }
                            else if ((cs.valueStart == 2 && c == '\'') || (cs.valueStart == 3 && c == '"'))
                            {
                                if (!cs.escapeChar)
                                {
                                    cs.valueStart = -2;
                                    return true;
                                }
                                else
                                {
                                    cs.escapeChar = false;
                                }
                            }

                        }
                    }
                    #endregion
                    break;
                case ':':
                    cs.CheckIsError(c);
                    #region 冒号
                    if (cs.jsonStart && cs.keyStart < 2 && cs.valueStart < 2 && cs.state == 0)
                    {
                        cs.keyStart = 0;
                        cs.state = 1;
                        return true;
                    }
                    #endregion
                    break;
                case ',':
                    cs.CheckIsError(c);
                    #region 逗号 {"a": [11,"22", ], "Type": 2}
                    if (cs.jsonStart && cs.keyStart < 2 && cs.valueStart < 2 && cs.state == 1)
                    {
                        cs.state = 0;
                        cs.valueStart = 0;
                        cs.setDicValue = true;
                        return true;
                    }
                    else if (cs.arrayStart && !cs.jsonStart) //[a,b]  [",",33] [{},{}]
                    {
                        if ((cs.state == -1 && cs.valueStart == -1) || (cs.valueStart < 2 && cs.state == 1))
                        {
                            cs.valueStart = 0;
                            return true;
                        }
                    }
                    #endregion
                    break;
                case ' ':
                case '\r':
                case '\n':
                case '\t':
                    if (cs.jsonStart && cs.keyStart <= 0 && cs.valueStart <= 0)
                    {
                        return true;//跳过空格。
                    }
                    break;
                default: //值开头。。
                    cs.CheckIsError(c);
                    if (c == '\\') //转义符号
                    {
                        if (cs.escapeChar)
                        {
                            cs.escapeChar = false;
                        }
                        else
                        {
                            cs.escapeChar = true;
                            //return true;
                        }
                    }
                    else
                    {
                        cs.escapeChar = false;
                    }
                    if (cs.jsonStart)
                    {
                        if (cs.keyStart <= 0 && cs.state <= 0)
                        {
                            cs.keyStart = 1;//无引号的
                        }
                        else if (cs.valueStart <= 0 && cs.state == 1)
                        {
                            cs.valueStart = 1;//无引号的
                        }
                    }
                    else if (cs.arrayStart)
                    {
                        cs.state = 1;
                        if (cs.valueStart < 1)
                        {
                            cs.valueStart = 1;//无引号的
                        }
                    }
                    break;
            }
            return false;
        }
        #endregion
        #endregion

        #region GetFieldPathValue
        /// <summary>
        /// 取对象属性值（如：id 、userInfo.id）
        /// </summary>
        /// <param name="input"></param>
        /// <param name="filedPath"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static object GetFieldPathValue(this object input, string filedPath, string typeName = null)
        {
            if (filedPath.IsNullOrEmpty() || input.IsNullOrEmpty())
                return null;
            //if (typeName.IsNullOrEmpty()) typeName = "string";

            var filedPaths = filedPath.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            object val = null;
            object newVal = input;
            for (var i = 0; i < filedPaths.Length; i++)
            {
                var filedName = filedPaths[i];
                if (filedName.IsNullOrEmpty()) continue;
                if (filedName.Contains("[") && filedName.Contains("]"))//取数组下标值
                {
                    var filedbame = filedName.Substring(0, filedName.IndexOf("["));
                    newVal = GetFieldValue(newVal, filedbame);
                    if (newVal != null)
                    {
                        var index = GetMatchValue(filedName).ChanageType<int>();
                        if (index >= 0 && newVal is System.Collections.IEnumerable array)
                        {
                            newVal = array.CastToList<object>()[i];
                        }
                    }
                }
                else//取属性值
                {
                    newVal = GetFieldValue(newVal, filedName);
                    if (newVal is string && newVal.NullToStr().IsJson())
                    {
                        newVal = Newtonsoft.Json.JsonConvert.DeserializeObject(newVal.NullToStr());
                    }
                    else if (newVal is Newtonsoft.Json.Linq.JValue _jValue && _jValue.Value is string && _jValue.Value.NullToStr().IsJson())
                    {
                        newVal = Newtonsoft.Json.JsonConvert.DeserializeObject(_jValue.Value.NullToStr());
                    }
                }

                if (i == filedPaths.Length - 1)
                {
                    val = newVal;
                }
            }
            if (val != null && typeName.IsNotNullOrEmpty())
            {
                var typeoff = GetTypeByName(typeName);
                if (typeoff != null)
                    return val.ChanageType(typeoff);
            }
            return val;
        }
        /// <summary>
        /// 取对象属性值
        /// </summary>
        /// <param name="input"></param>
        /// <param name="filedName"></param>
        /// <returns></returns>
        public static object GetFieldValue(this object input, string filedName)
        {
            if (filedName.IsNullOrEmpty() || input.IsNullOrEmpty())
                return null;
            filedName = filedName.Trim();
            if (input is Newtonsoft.Json.Linq.JObject _jObject)//JSON
            {
                foreach (var item in _jObject)
                {
                    if (item.Key.Trim().ToLower() != filedName.ToLower()) continue;
                    return item.Value;
                }
            }
            else if (input is Newtonsoft.Json.Linq.JArray _ajObject)
            {
                return input;
            }
            else if (input is System.Dynamic.ExpandoObject _eObject)//ExpandoObject
            {
                foreach (var item in _eObject)
                {
                    if (item.Key.Trim().ToLower() != filedName.ToLower()) continue;
                    return item.Value;
                }
            }
            else if (input is System.Collections.Generic.Dictionary<string, object> _dicObject)//Dictionary
            {
                foreach (var item in _dicObject)
                {
                    if (item.Key.Trim().ToLower() != filedName.ToLower()) continue;
                    return item.Value;
                }
            }
            else if (input is System.Collections.Generic.Dictionary<string, string> _dicstr)//Dictionary
            {
                foreach (var item in _dicstr)
                {
                    if (item.Key.Trim().ToLower() != filedName.ToLower()) continue;
                    return item.Value;
                }
            }
            else if (input is string)//string
            {
                return input;
            }
            else//实体
            {
                var property = input.GetType().GetProperties().ToList().Find(item => item.Name.Trim().ToLower() != filedName.ToLower());
                if (property != null)
                {
                    return property.GetValue(input);
                }
                var field = input.GetType().GetFields().ToList().Find(item => item.Name.Trim().ToLower() != filedName.ToLower());
                if (field != null)
                {
                    return field.GetValue(input);
                }
            }
            return null;
        }
        #endregion

        #region GetMatchValue
        /// <summary>
        /// 取中括号[]值
        /// </summary>
        /// <param name="template"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static string GetMatchValue(string template, string left = "[", string right = "]")
        {
            var rgx = new System.Text.RegularExpressions.Regex(@"(?i)(?<=\" + left + @")(.*)(?=\" + right + ")");//中括号[]
            var rt = rgx.Match(template).Value;//中括号[]
            return rt;
        }
        #endregion

        #region GetXmlNodeAttributeValue
        /// <summary>
        /// 选择xmlNode节点的匹配xmlAttributeName的属性XmlAttribute.
        /// </summary>
        /// <param name="xmlNode">X节点</param>
        /// <param name="xmlAttributeName">要匹配xmlAttributeName的属性名称</param>
        /// <returns>返回xmlAttributeName</returns>
        public static string GetXmlNodeAttributeValue(this XmlNode xmlNode, string xmlAttributeName, string defValue = "")
        {
            try
            {
                if (xmlNode != null && xmlNode.Attributes.Count > 0)
                {
                    var Attr = xmlNode.Attributes[xmlAttributeName];
                    if (Attr != null)
                    {
                        var val = Attr.Value;
                        if (!val.IsNullOrEmpty()) return val;
                    }
                }
            }
            catch
            {
            }
            return defValue;
        }

        /// <summary>
        /// 获取列值
        /// </summary>
        /// <param name="dr">DataRow</param>
        /// <param name="colName">列名称</param>
        /// <returns>列值</returns>
        public static string GetDataRowColumnValue(this DataRow dr, string colName, string defValue = "")
        {
            try
            {
                if (dr == null || colName.IsNullOrEmpty() || dr.Table == null) return defValue;
                if (dr.Table.Columns.Contains(colName))
                {
                    if (dr[colName].IsNullOrEmpty()) return defValue;
                    return dr[colName].NullToStr();
                }
            }
            catch
            {
            }
            return defValue;
        }
        public static string GetDynamicAttributeValue(dynamic _expando, string proName, string defValue = "")
        {
            try
            {
                if (_expando == null || proName.IsNullOrEmpty()) return defValue;
                IDictionary<string, object> dy = _expando;
                if (dy == null) return "";
                proName = proName.Trim().ToLower();
                foreach (var dic in dy)
                {
                    if (dic.Key.Trim().ToLower() == proName)
                    {
                        return dic.Value.NullToStr();
                    }
                }
            }
            catch
            {
            }
            return defValue;
        }
        #endregion
    }
}
