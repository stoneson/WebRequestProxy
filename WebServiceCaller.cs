
using HCenter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
namespace WebRequestProxy
{
    /// <summary>
    /// ����WebRequest/WebResponse����WebService/WCF���õ���
    /// </summary>
    public class WebServiceCaller
    {
        #region Query
        /// <summary>
        /// ��Զ����������ѯ����
        /// </summary>
        /// <param name="methodName">���÷�����</param>
        /// <param name="jsonStr">json �ַ���</param>
        /// <returns>��ѯ�������</returns>
        public static T Query<T>(String url, string methodName, string jsonStr, IDictionary<string, string> headers = null)
        {
            var ret = Query(url, methodName, jsonStr, headers);
            return ret.ChanageType<T>();
        }

        /// <summary>
        /// ��Զ����������ѯ����
        /// </summary>
        /// <param name="methodName">���÷�����</param>
        /// <param name="jsonStr">json �ַ���</param>
        /// <returns>��ѯ�������</returns>
        public static object Query(String url, string methodName, string jsonStr, IDictionary<string, string> headers = null)
        {
            var jObject = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonStr);
            return Query(url, methodName, jObject, headers);
        }
        /// <summary>
        /// ��Զ����������ѯ����
        /// </summary>
        /// <param name="methodName">���÷�����</param>
        /// <param name="jObject">JSON����</param>
        /// <returns>��ѯ�������</returns>
        public static T Query<T>(String url, string methodName, object jObject, IDictionary<string, string> headers = null)
        {
            var ret = Query(url, methodName, jObject, headers);
            return ret.ChanageType<T>();
        }

        /// <summary>
        /// ��Զ����������ѯ����
        /// </summary>
        /// <param name="methodName">���÷�����</param>
        /// <param name="jObject">JSON����</param>
        /// <returns>��ѯ�������</returns>
        public static object Query(String url, string methodName, object jObject, IDictionary<string, string> headers = null)
        {
            var pars = GetDicParameters(jObject, GetMethodInputs(url, methodName));
            var ret = Query(url, methodName, pars, headers);
            return ret;
        }

        /// <summary>
        /// ��Զ����������ѯ����
        /// </summary>
        /// <param name="methodName">���÷�����</param>
        /// <param name="pars">��μ���</param>
        /// <returns>��ѯ�������</returns>
        public static T Query<T>(String url, string methodName, IDictionary<string, object> pars, IDictionary<string, string> headers = null)
        {
            var ret = Query(url, methodName, pars, headers);
            return ret.ChanageType<T>();
        }

        /// <summary>
        /// ��Զ����������ѯ����
        /// </summary>
        /// <param name="methodName">���÷�����</param>
        /// <param name="pars">��μ���</param>
        /// <returns>��ѯ�������</returns>
        public static object Query(String url, string methodName, IDictionary<string, object> pars, IDictionary<string, string> headers = null)
        {
            var returnValueDoc = QuerySoapWebService(url, methodName, pars, headers);
            return XmlDocumentToObj(returnValueDoc, GetMethodOutput(url, methodName));
        }
        #endregion

        #region QueryPostWebService
        /// <summary>
        /// ��ҪWebService֧��Post����
        /// </summary>
        public static XmlDocument QueryPostWebService(string url, string methodName, IDictionary<string, object> pars, IDictionary<string, string> headers)
        {
            url = getUrl(url);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url + "/" + methodName);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8"; //"application/x-www-form-urlencoded";
            //����HTTP ��ͷ������/ֵ�Եļ���
            if (headers != null && headers.Count > 0)
            {
                BuildHeader(request, headers);
            }
            SetWebRequest(request);
            byte[] data = EncodePars(pars);
            WriteRequestData(request, data);
            return ReadXmlResponse(request.GetResponse());
        }
        #endregion
        #region QueryGetWebService
        /// <summary>
        /// ��ҪWebService֧��Get����
        /// </summary>
        public static XmlDocument QueryGetWebService(string url, string methodName, IDictionary<string, object> pars, IDictionary<string, string> headers)
        {
            url = getUrl(url);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url + "/" + methodName + "?" + ParsToString(pars));
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8"; //"application/x-www-form-urlencoded";
            //����HTTP ��ͷ������/ֵ�Եļ���
            if (headers != null && headers.Count > 0)
            {
                BuildHeader(request, headers);
            }
            SetWebRequest(request);
            return ReadXmlResponse(request.GetResponse());
        }
        #endregion
        #region QuerySoapWebService
        /// <summary>
        /// ͨ��SOAPЭ�鶯̬����WebService/WCF 
        /// </summary>
        /// <param name="url">WebService/WCF ��ַ,�����WCF��ַ��׺һ��Ҫ�ӡ�?singleWsdl��</param>
        /// <param name="methodName"> ���÷����� </param>
        /// <param name="pars"> ������</param>
        /// <param name="xmlNs"> ���ֿռ�</param>
        /// <returns> �����</returns>
        public static XmlDocument QuerySoapWebService(String url, String methodName, IDictionary<string, object> pars, IDictionary<string, string> headers = null)
        {
            //loadWSDL(url);
            string xmlNs = GetNamespace(url);
            var sso = GetServiceOperation(url, methodName);

            url = getUrl(url);

            // ��ȡ�������
            var request = (HttpWebRequest)HttpWebRequest.Create(url);
            // ��������head
            request.Method = "POST";
            request.ContentType = "text/xml; charset=utf-8";
            request.Accept = request.ContentType;
            //request.Headers.Add("SOAPAction", "\"" + xmlNs + (xmlNs.EndsWith("/") ? "" : "/") + methodName + "\"");
            request.Headers.Add("SOAPAction", string.IsNullOrWhiteSpace(sso.SoapAction) ?
                ("\"" + xmlNs + (xmlNs.EndsWith("/") ? "" : "/") + methodName + "\"") : sso.SoapAction);
            //����HTTP ��ͷ������/ֵ�Եļ���
            if (headers != null && headers.Count > 0)
            {
                BuildHeader(request, headers);
            }
            // �����������
            SetWebRequest(request);
            // ��ȡsoapЭ��
            byte[] data = EncodeParsToSoap(pars, xmlNs, methodName, sso);
            request.ContentLength = data.Length;
            // ��soapЭ��д������
            WriteRequestData(request, data);

            var returnDoc = new XmlDocument();
            var returnValueDoc = new XmlDocument();
            // ��ȡ�������Ӧ
            returnDoc = ReadXmlResponse(request.GetResponse());

            var mgr = new XmlNamespaceManager(returnDoc.NameTable);
            mgr.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
            // ���ؽ��
            var resBody = returnDoc.SelectSingleNode("//soap:Body/*/*", mgr);
            string RetXml = resBody?.InnerXml;

            returnValueDoc.LoadXml("<root>" + RetXml + "</root>");
            AddDelaration(returnValueDoc);

            /*  System.Data.DataSet ds = new System.Data.DataSet();
              XmlNodeReader reader = new XmlNodeReader(returnValueDoc);
              ds.ReadXml(reader);*/
            // return returnValueDoc.OuterXml;

            return returnValueDoc;

            //return XmlDocumentToObj(returnValueDoc, sso.Output);
        }
        #endregion

        #region private static
        #region XmlDocumentToObj
        static object XmlDocumentToObj(XmlDocument doc, OperationInputOutput output)
        {
            if (output != null)
            {
                if (output.isComplexType || output.tnsTypes?.Count > 0)
                {
                    var ds = new System.Data.DataSet();
                    try
                    {
                        using (XmlNodeReader reader = new XmlNodeReader(doc))
                        {
                            ds.ReadXml(reader);
                        }
                    }
                    catch { }
                    if (ds != null && ds.Tables.Count > 0 && output.isArrayOf)
                    {
                        //var itemsStr = Newtonsoft.Json.JsonConvert.SerializeObject(ds.Tables[0]);
                        var items = ds.Tables[0].ToDynamic();//Newtonsoft.Json.JsonConvert.DeserializeObject(itemsStr);
                        return items;
                    }
                    else if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0 && output.isArrayOf == false)
                    {
                        //var itemsStr = Newtonsoft.Json.JsonConvert.SerializeObject(ds.Tables[0].Rows[0]);
                        var items = ds.Tables[0].Rows[0].ToDynamic();// Newtonsoft.Json.JsonConvert.DeserializeObject(itemsStr);
                        return items;
                    }
                }
            }
            //--------------------------------------------------------------------------------------------------------------
            if (!string.IsNullOrWhiteSpace(doc.InnerText) && doc.InnerText.NullToStr().IsJson())
            {
                var items = Newtonsoft.Json.JsonConvert.DeserializeObject(doc.InnerText);
                return items;
            }
            return doc.InnerText;
        }
        #endregion
        #region GetDicParameters
        private static IDictionary<string, object> GetDicParameters(object inputObj, List<OperationInputOutput> inputs=null)
        {
            var pars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (inputObj.IsNullOrEmpty() )
                return pars;
            //------------------------------------------------------------------------------------
            if (inputs?.Count == 1)
            {
                var val = PubFun.GetFieldValue(inputObj, inputs[0].name);
                if (val != null)
                {
                    setKeyValue(pars, inputs[0].name, val, val.GetType().FullName);
                    return pars;
                }
                else
                {
                    setKeyValue(pars, inputs[0].name, inputObj, inputObj.GetType().FullName);
                    return pars;
                }
            }
            else if (inputs?.Count > 1)
            {
                inputs.ForEach(input=>
                {
                    var val = PubFun.GetFieldValue(inputObj, input.name);
                    if (val != null)
                    {
                        setKeyValue(pars, input.name, val, val.GetType().FullName);
                    }
                });
                return pars;
            }
            //------------------------------------------------------------------------------------
            if (inputObj is Newtonsoft.Json.Linq.JObject _jObject)//JSON
            {
                foreach (var item in _jObject)
                {
                    setKeyValue(pars, item.Key, item.Value, item.Value?.GetType().FullName);
                }
            }
            else if (inputObj is Newtonsoft.Json.Linq.JArray _ajObject)
            {
                return pars;
            }
            else if (inputObj is System.Dynamic.ExpandoObject _eObject)//ExpandoObject
            {
                foreach (var item in _eObject)
                {
                    setKeyValue(pars, item.Key, item.Value, item.Value?.GetType().FullName);
                }
            }
            else if (inputObj is System.Collections.Generic.Dictionary<string, string> _dicObject)//Dictionary
            {
                foreach (var item in _dicObject)
                {
                    setKeyValue(pars, item.Key, item.Value, item.Value?.GetType().FullName);
                }
            }
            else if (inputObj is string)//string
            {
                return pars;
            }
            else//ʵ��
            {
                var property = inputObj.GetType().GetProperties().ToList();
                property?.ForEach(p => { setKeyValue(pars, p.Name, p.GetValue(inputObj), p.PropertyType.FullName); });
                var field = inputObj.GetType().GetFields().ToList();
                field?.ForEach(p => { setKeyValue(pars, p.Name, p.GetValue(inputObj), p.FieldType.FullName); });
            }
            return pars;
        }
        private static void setKeyValue(Dictionary<string, object> pars, string key, object val, string typeName = null)
        {
            if (pars.IsNullOrEmpty() || key.IsNullOrEmpty() || val == null)
                return;

            //if (val != null && typeName.IsNotNullOrEmpty())
            //{
            //    var typeoff = PubFun.GetTypeByName(typeName);
            //    if (typeoff != null)
            //        val = val.ChanageType(typeoff);
            //}
            pars[key] = val;
        }
        #endregion
         
        #region EncodePars
        /// <summary>
        /// ��ȡ�ַ�����UTF8���ַ���
        /// </summary>
        /// <param name="pars"></param>
        /// <returns></returns>
        private static byte[] EncodePars(IDictionary<string, object> pars)
        {
            return Encoding.UTF8.GetBytes(ParsToString(pars));
        }
        /// <summary>
        /// ��Hashtableת����WEB�����ֵ���ַ���
        /// </summary>
        /// <param name="pars"></param>
        /// <returns></returns>
        private static string ParsToString(IDictionary<string, object> pars)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string k in pars.Keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(System.Web.HttpUtility.UrlEncode(k) + "=" + System.Web.HttpUtility.UrlEncode(pars[k].ToString()));
            }
            return sb.ToString();
        }
        #endregion
        #region BuildHeader
        private static void BuildHeader(HttpWebRequest request, IDictionary<string, string> headers)
        {
            if (request == null || headers == null) return;

            if (headers.ContainsKey("Accept"))
                headers.Remove("Accept");
            if (headers.ContainsKey("Accept-Encoding"))
                headers.Remove("Accept-Encoding");
            headers["Content-Type"] = "text/xml; charset=utf-8";
            //headers["Accept-Encoding"] = "gzip, deflate";
            headers["Expect"] = "100-continue";

            IEnumerator<KeyValuePair<string, string>> dem = headers.GetEnumerator();
            while (dem.MoveNext())
            {
                string name = dem.Current.Key;
                string value = dem.Current.Value;
                // ���Բ����������ֵΪ�յĲ���
                if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(value))
                {
                    request.Headers[name] = value;
                }
            }
        }

        /// <summary>
        /// ����soapheader�ڵ�
        /// </summary>
        /// <param name="doc"> soap�ĵ�</param>
        private static void InitSoapHeader(XmlDocument doc)
        {
            // ���soapheader�ڵ�
            XmlElement soapHeader = doc.CreateElement("soap", "Header", "http://schemas.xmlsoap.org/soap/envelope/");
            //XmlElement soapId = doc.CreateElement("userid");
            //soapId.InnerText = ID;
            //XmlElement soapPwd = doc.CreateElement("userpwd");
            //soapPwd.InnerText = PWD;
            //soapHeader.AppendChild(soapId);
            //soapHeader.AppendChild(soapPwd);
            doc.ChildNodes[0].AppendChild(soapHeader);
        }
        #endregion

        #region EncodeParsToSoap
        /// <summary>
        /// �����ֽ��������ʽ����soapЭ��
        /// </summary>
        /// <param name="pars"> ������</param>
        /// <param name="xmlNs"> ���ֿռ�</param>
        /// <param name="methodName"> ������</param>
        /// <returns> �ֽ�����</returns>
        private static byte[] EncodeParsToSoap(IDictionary<string, object> pars, String xmlNs, String methodName, ServiceOperation sso)
        {
            XmlDocument doc = new XmlDocument();
            // ����soap�ĵ�
            doc.LoadXml("<soap:Envelope xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance/\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\"></soap:Envelope>");

            // ����soapbody�ڵ�
            InitSoapHeader(doc);

            // ����soapbody�ڵ�
            XmlElement soapBody = doc.CreateElement("soap", "Body", "http://schemas.xmlsoap.org/soap/envelope/");
            // ����Ҫ���õķ�������һ�������ڵ�
            XmlElement soapMethod = doc.CreateElement(methodName);
            soapMethod.SetAttribute("xmlns", xmlNs);
            // �����������еĲ�����
            var dicpars = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var dic in pars) dicpars[dic.Key] = dic.Value;
            // ���ݲ������еļ�ֵ�ԣ�����һ�������ڵ㣬�����뷽���ڵ���
            if (sso != null && sso.Inputs?.Count > 0)
            {
                sso.Inputs.ForEach(input =>
                {
                    var soapPar = doc.CreateElement(input.name);
                    var val = dicpars[input.name];
                    setXmlNodeInnerXml(doc, soapPar,null, val, input);
                    #region old
                    //if (input.isComplexType || input.tnsTypes?.Count > 0)
                    //{
                    //    if (val is Newtonsoft.Json.Linq.JObject jobj)
                    //    {
                    //        var dock = ConvertJObjectToXmlDoc(jobj, input.name);
                    //        if (dock != null && dock.FirstChild != null)
                    //            soapPar.InnerXml = dock.FirstChild.InnerXml;
                    //        else
                    //            soapPar.InnerXml = ConvertJObjectToXml(jobj, input.name);
                    //    }
                    //    else if (val is Newtonsoft.Json.Linq.JArray jarr)
                    //    {
                    //        var dock = ConvertJObjectToXmlDoc(jarr, input.name, input.tnsTypes?.Count > 0 ? input.tnsTypes[0].tnsType : input.tnsType);
                    //        if (dock != null && dock.FirstChild != null)
                    //            soapPar.InnerXml = dock.FirstChild.InnerXml;
                    //        else
                    //            soapPar.InnerXml = ConvertJObjectToXml(jarr, input.name, input.tnsTypes?.Count > 0 ? input.tnsTypes[0].tnsType : input.tnsType);
                    //    }
                    //    else
                    //    {
                    //        soapPar.InnerXml = val.NullToStr();
                    //    }
                    //}
                    //else
                    //{
                    //    soapPar.InnerXml = val.NullToStr();
                    //}
                    #endregion
                    if (!string.IsNullOrWhiteSpace(input.xmlns))
                    {
                        var prex = "d4p1";
                        soapPar.SetAttribute("xmlns:" + prex, input.xmlns);

                        var newNode = new StringBuilder();
                        getChildNodesInnerXml(doc, soapPar, newNode, input.xmlns, prex);

                        soapPar.InnerXml = newNode.ToString();
                    }
                    soapMethod.AppendChild(soapPar);
                });
            }
            else
            {
                #region foreach (var dic in pars
                foreach (var dic in pars)
                {
                    var soapPar = doc.CreateElement(dic.Key);
                    var val = dic.Value;
                    var valtype = val?.GetType();
                    if (valtype != null && valtype.IsClass && val is Newtonsoft.Json.Linq.JObject jobj)
                    {
                        var dock = ConvertJObjectToXmlDoc(jobj, dic.Key);
                        if (dock != null && dock.FirstChild != null)
                            soapPar.InnerXml = dock.FirstChild.InnerXml;
                        else
                            soapPar.InnerXml = ConvertJObjectToXml(jobj, dic.Key);
                    }
                    else if (valtype != null && valtype.IsClass && val is Newtonsoft.Json.Linq.JArray jarr)
                    {
                        var dock = ConvertJObjectToXmlDoc(jarr, dic.Key, dic.Key);
                        if (dock != null && dock.FirstChild != null)
                            soapPar.InnerXml = dock.FirstChild.InnerXml;
                        else
                            soapPar.InnerXml = ConvertJObjectToXml(jarr, dic.Key, dic.Key);
                    }
                    else if (valtype != null && valtype.IsClass && !valtype.Equals(typeof(string)) && !valtype.Equals(typeof(Newtonsoft.Json.Linq.JValue)))
                    {
                        soapPar.InnerXml = ObjectToSoapXml(val);
                    }
                    else
                    {
                        soapPar.InnerXml = val.NullToStr();
                    }
                    soapMethod.AppendChild(soapPar);
                }
                #endregion
            }
            // soapbody�ڵ��м��뷽���ڵ�
            soapBody.AppendChild(soapMethod);

            // soap�ĵ��м���soapbody�ڵ�
            doc.DocumentElement.AppendChild(soapBody);

            // �������
            AddDelaration(doc);

            // ����Ĳ�����DataSet���ͣ����������л����XML�е�diffgr:diffgram/NewDataSet�ڵ��xmlns='' �����޷�ȡ��ÿ�еļ�¼��
            XmlNode node = doc.DocumentElement.SelectSingleNode("//NewDataSet");
            if (node != null)
            {
                XmlAttribute attr = doc.CreateAttribute("xmlns");
                attr.InnerText = "";
                node.Attributes.Append(attr);
            }
            // ���ֽ��������ʽ����soap�ĵ�
            var retsxml = doc.OuterXml;
            return Encoding.UTF8.GetBytes(retsxml);
        }

        /// <summary>
        /// ����
        /// </summary>
        /// <param name="node"></param>
        /// <param name="xmlns"></param>
        private static void setXmlNodeInnerXml(XmlDocument xmlDoc, XmlElement psoapPar, XmlElement subsoapPar, object inputVal, OperationInputOutput input,bool isFirst=false,int level=0)
        {
            if (xmlDoc == null || inputVal == null || psoapPar == null || input == null) return;

            if (input.tnsTypes?.Count > 0)
            {
                input.tnsTypes.ForEach(subInput=>
                {
                    var val = isFirst || (input.isArrayOf && !subInput.isComplexType)? inputVal: PubFun.GetFieldValue(inputVal, subInput.name);
                    if (val is Newtonsoft.Json.Linq.JObject jobj)
                    {
                        var soapPar = xmlDoc.CreateElement(subInput.name);
                        if (subInput.tnsTypes?.Count > 0)
                        {
                            setXmlNodeInnerXml(xmlDoc, soapPar, null, val, subInput, false, level + 1);
                        }
                        else
                        {
                            var dock = ConvertJObjectToXmlDoc(jobj, subInput.name);
                            if (dock != null && dock.FirstChild != null)
                                soapPar.InnerXml = dock.FirstChild.InnerXml;
                            else
                                soapPar.InnerXml = ConvertJObjectToXml(jobj, subInput.name);
                        }
                        psoapPar.AppendChild(soapPar);
                    }
                    else if (val is Newtonsoft.Json.Linq.JArray jarr)
                    {
                        if (subInput.tnsTypes?.Count > 0)
                        {
                            var isadd = false;
                            var asoapPar = subsoapPar;
                            foreach (var item in jarr)
                            {
                                var jrobj = item.Value<object>();
                                if (jrobj != null)
                                {
                                    if (input.isArrayOf &&
                                    ((subInput.isArrayOf && subInput.tnsTypes?.Count == 1 && subInput.tnsTypes[0].isArrayOf && isadd == false )
                                    || (!isFirst && level == 0)))
                                    {
                                        asoapPar = xmlDoc.CreateElement(subInput.name);
                                    }
                                    if (asoapPar == null)
                                    {
                                        asoapPar = xmlDoc.CreateElement(subInput.name);
                                        psoapPar.AppendChild(asoapPar);
                                    }
                                    setXmlNodeInnerXml(xmlDoc, asoapPar, asoapPar,jrobj, subInput
                                        , !subInput.isArrayOf || (subInput.isArrayOf && subInput.tnsTypes?.Count == 1 && subInput.tnsTypes[0].isArrayOf)
                                        ,level +1);
                                    if (input.isArrayOf &&
                                     ((subInput.isArrayOf && subInput.tnsTypes?.Count == 1 && subInput.tnsTypes[0].isArrayOf && isadd == false)
                                     || (!isFirst && level == 0)))
                                    {
                                        isadd = true;
                                        psoapPar.AppendChild(asoapPar);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var soapPar = xmlDoc.CreateElement(subInput.name);
                            var dock = ConvertJObjectToXmlDoc(jarr, input.name, input.tnsTypes?.Count > 0 ? input.tnsTypes[0].tnsType : input.tnsType);
                            if (dock != null && dock.FirstChild != null)
                                soapPar.InnerXml = dock.FirstChild.InnerXml;
                            else
                                soapPar.InnerXml = ConvertJObjectToXml(jarr, input.name, input.tnsTypes?.Count > 0 ? input.tnsTypes[0].tnsType : input.tnsType);
                            psoapPar.AppendChild(soapPar);
                        }
                    }
                    else
                    {
                        var soapPar = xmlDoc.CreateElement(subInput.name);
                        try
                        {
                            if (val is Newtonsoft.Json.Linq.JValue)
                            {
                                var typeName = subInput.tnsType;
                                if (val != null && typeName.IsNotNullOrEmpty())
                                {
                                    var typeoff = PubFun.GetTypeByName(typeName);
                                    if (typeoff != null)
                                        val = val.ChanageType(typeoff);
                                }
                                if (subInput.tnsType.Equals("datetime", StringComparison.OrdinalIgnoreCase))
                                {
                                    val = Convert.ToDateTime(val).GetDateTimeFormats('s')[0];
                                }
                            }
                        }
                        catch { }
                        soapPar.InnerXml = val.NullToStr();
                        psoapPar.AppendChild(soapPar);
                    }
                });
                
            }
            else
            {
                psoapPar.InnerXml = inputVal.NullToStr();
            }
        }
        private static void getChildNodesInnerXml(XmlDocument xmlDoc, XmlNode node, StringBuilder newNode, string xmlns, string prex = "a")
        {
            if (node == null || newNode == null || string.IsNullOrWhiteSpace(xmlns)) return;

            if (node.HasChildNodes)
            {
                foreach (XmlNode node1 in node.ChildNodes)
                {
                    if (node1.NodeType != XmlNodeType.Element) continue;

                    //if (node1.HasChildNodes && node1.ChildNodes[0].NodeType == XmlNodeType.Element)
                    //{
                    //    newNode.Append($"<{prex}:{node1.Name} xmlns:a=\"{xmlns}\"");
                    //}
                    //else
                    {
                        newNode.Append($"<{prex}:{node1.Name}");
                    }
                    if (node1.Attributes != null)
                    {
                        foreach (XmlAttribute xab in node1.Attributes)
                        {
                            newNode.Append($" {xab.Name}={xab.Value}");
                        }
                    }
                    newNode.Append(">");
                    //----------------------------------------------------------------------------------------
                    if (node1.HasChildNodes && node1.ChildNodes[0].NodeType == XmlNodeType.Element)
                    {
                        getChildNodesInnerXml(xmlDoc, node1, newNode, xmlns, prex);

                        newNode.Append($"</{prex}:{node1.Name}>");
                    }
                    else
                    {
                        newNode.Append($"{node1.InnerText}</{prex}:{node1.Name}>");
                    }
                    //newNode.AppendLine();
                }
            }
        }
        #endregion
        #region ConvertJObjectToXml
        private static string ConvertJObjectToXml(Newtonsoft.Json.Linq.JObject jo, string rootElementName)
        {
            var doc = ConvertJObjectToXmlDoc(jo, rootElementName);
            return ConvertXmlDocumentToStr(jo, doc);
        }
        private static string ConvertJObjectToXml(Newtonsoft.Json.Linq.JArray jo, string rootElementName, string itemElementName)
        {
            var doc = ConvertJObjectToXmlDoc(jo, rootElementName, itemElementName);
            return ConvertXmlDocumentToStr(jo, doc);
        }
        private static XmlDocument ConvertJObjectToXmlDoc(Newtonsoft.Json.Linq.JObject jo, string rootElementName)
        {
            var doc = JsonConvert.DeserializeXmlNode(jo.ToString(), rootElementName);
            return doc;
        }
        private static XmlDocument ConvertJObjectToXmlDoc(Newtonsoft.Json.Linq.JArray jo, string rootElementName, string itemElementName)
        {
            var jobj = new Newtonsoft.Json.Linq.JObject();
            jobj[itemElementName] = jo;
            var doc = JsonConvert.DeserializeXmlNode(jobj.ToString(), rootElementName);
            return doc;
        }
        //private static XmlDocument ConvertObjectToXmlDoc(object obj, string rootElementName=null)
        //{
        //    XmlDocument doc = new XmlDocument();
        //    if (obj is Newtonsoft.Json.Linq.JObject jo)
        //    {
        //        doc = JsonConvert.DeserializeXmlNode(jo.ToString(), rootElementName);
        //        return doc;
        //    }
        //    if (obj is Newtonsoft.Json.Linq.JArray jarr)
        //    {
        //        doc = ConvertJObjectToXmlDoc(jarr, rootElementName);
        //        return doc;
        //    }
        //    else
        //    {
        //        var mySerializer = new XmlSerializer(obj.GetType());
        //        var ms = new MemoryStream();
        //        mySerializer.Serialize(ms, obj);
        //        doc.LoadXml(Encoding.UTF8.GetString(ms.ToArray()));

        //    }
        //    return doc;
        //}
        /// <summary>
        /// �����������е�����ȡ��
        /// </summary>
        /// <param name="o">����ֵ����</param>
        /// <returns>�ַ���ֵ����</returns>
        private static string ObjectToSoapXml(object o)
        {
            XmlSerializer mySerializer = new XmlSerializer(o.GetType());
            MemoryStream ms = new MemoryStream();
            mySerializer.Serialize(ms, o);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Encoding.UTF8.GetString(ms.ToArray()));
            return ConvertXmlDocumentToStr(o, doc);
        }
        private static string ConvertXmlDocumentToStr(object o, XmlDocument doc)
        {
            if (doc == null) return "";
            //var sb = new StringBuilder();
            //var sr = new StringWriter(sb);
            //var xw = new XmlTextWriter(sr);
            //xw.Formatting = System.Xml.Formatting.Indented;
            //doc.WriteTo(xw);
            //return sb.ToString();
            if (doc.DocumentElement != null)
            {
                return doc.DocumentElement.InnerXml;
            }
            else
            {
                return o.ToString();
            }
        }
        #endregion

        #region SetCertificatePolicy
        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="request"> ����</param>
        private static void SetWebRequest(HttpWebRequest request)
        {
            //����Э������ǰ����Э��汾
            request.ProtocolVersion = HttpVersion.Version11;
            //request.Credentials = CredentialCache.DefaultCredentials;
            // SetCertificatePolicy();
            //request.Timeout = 10000;
        }
        static WebServiceCaller()
        {
            SetCertificatePolicy();
        }
        private static void SetCertificatePolicy()
        {
            //�����֤֤��Ļص�����
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            // ����������Э�����͡�
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = 1000;
            ServicePointManager.Expect100Continue = false;
            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072 | SecurityProtocolType.Tls | SecurityProtocolType.Ssl3; //SecurityProtocolType.Tls;// (SecurityProtocolType)3072; // 
            //ServicePointManager.CheckCertificateRevocationList = true;
            //ServicePointManager.DefaultConnectionLimit = 100;
            //ServicePointManager.Expect100Continue = false;
        }

        /// <summary>
        /// Remotes the certificate validate.
        /// </summary>
        private static bool ValidateServerCertificate(object sender, System.Security.Cryptography.X509Certificates.X509Certificate cert, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }
        /// <summary>
        /// ��soapЭ��д������
        /// </summary>
        /// <param name="request"> ����</param>
        /// <param name="data"> soapЭ��</param>
        private static void WriteRequestData(HttpWebRequest request, byte[] data)
        {
            request.ContentLength = data.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(data, 0, data.Length);
            writer.Close();
        }
        #endregion

        #region ReadXmlResponse
        /// <summary>
        /// ����Ӧ�����ȡΪxml����
        /// </summary>
        /// <param name="response"> ��Ӧ����</param>
        /// <returns> xml����</returns>
        private static XmlDocument ReadXmlResponse(WebResponse response)
        {
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            String retXml = sr.ReadToEnd();
            sr.Close();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(retXml);
            return doc;
        }

        /// <summary>
        /// ��xml�ĵ��������
        /// </summary>
        /// <param name="doc"> xml�ĵ�</param>
        private static void AddDelaration(XmlDocument doc)
        {
            XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.InsertBefore(decl, doc.DocumentElement);
        }

        private static string getUrl(string url)
        {
            if (url.ToUpper().EndsWith("?WSDL"))
            {
                url = url.Substring(0, url.Length - "?WSDL".Length);
            }
            else if (url.ToUpper().EndsWith("?SINGLEWSDL"))
            {
                url = url.Substring(0, url.Length - "?SINGLEWSDL".Length);
            }
            return url;
        }
        #endregion
        #endregion

        #region loadWSDL ServiceSoap
        /// <summary>
        /// ���������Ϣ
        /// </summary>
        //private static Dictionary<string, ServiceSoap> DIC_ServiceSoap = new Dictionary<string, ServiceSoap>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// ��ȡ������Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static ServiceSoap GetServiceSoap(String url)
        {
            //ServiceSoap ssp;
            //if (DIC_ServiceSoap.TryGetValue(url, out ssp) == false || ssp == null)
            //{
            //    ssp = loadWSDL(url);
            //}

            return HCenter.CommonUtils.Cache.MemoryCacheHelper.Instance.Get<ServiceSoap>(url, () =>
            {
                var ssp = loadWSDL(url);

                HCenter.CommonUtils.Cache.MemoryCacheHelper.Instance.Set(getUrl(url), ssp, HCenter.CommonUtils.Cache.ExpiresTime.Minutes_30);
                return ssp;
            },
            HCenter.CommonUtils.Cache.ExpiresTime.Minutes_30);
        }
        /// <summary>
        /// ��ȡwsdl�е����ֿռ�
        /// </summary>
        /// <param name="url"> wsdl��ַ</param>
        /// <returns> ���ֿռ�</returns>
        private static string GetNamespace(String url)
        {
            var ssp = GetServiceSoap(url);
            return ssp?.Namespace;
        }
        /// <summary>
        /// ��ȡ������Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static ServiceOperation GetServiceOperation(String url, String methodName)
        {
            var ssp = GetServiceSoap(url);
            ServiceOperation so;
            if (ssp.DicOperations.TryGetValue(methodName, out so) == false || so == null)
            {
                so = new ServiceOperation() { Name = methodName };
            }
            return so;
        }

        /// <summary>
        /// ��ȡ���������Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static List<OperationInputOutput> GetMethodInputs(String url, String methodName)
        {
            var so = GetServiceOperation(url, methodName);
            return so.Inputs;
        }
        /// <summary>
        /// ��ȡ����������Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static OperationInputOutput GetMethodOutput(String url, String methodName)
        {
            var so = GetServiceOperation(url, methodName);
            return so.Output;
        }

        private static XmlDocument loadXmlDocument(String url)
        {
            try
            {
                // ����wsdl������󣬲����ж�ȡ���ֿռ�
                var request = (HttpWebRequest)WebRequest.Create(url);
                SetWebRequest(request);
                var response = request.GetResponse();
                var sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                var doc = new XmlDocument();
                doc.LoadXml(sr.ReadToEnd());
                sr.Close();
                return doc;
            }
            catch { }
            return new XmlDocument();
        }
        /// <summary>
        /// ����WSDL������Ϣ
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static ServiceSoap loadWSDL(String url)
        {
            // ����wsdl������󣬲����ж�ȡ���ֿռ�
            var doc = loadXmlDocument(url.ToUpper().EndsWith("?WSDL") || url.ToUpper().EndsWith("?SINGLEWSDL") ? url : $"{url}?WSDL");
            //--------------------------------------------------------------------------------------------------------------------------------------------------------
            var ssp = new ServiceSoap();

            #region sevicesNode
            var sevicesNode = doc.DocumentElement.GetElementsByTagName("wsdl:service");
            if (sevicesNode == null || sevicesNode.Count == 0)
                sevicesNode = doc.DocumentElement.GetElementsByTagName("service");
            if (sevicesNode?.Count > 0)
            {
                var saas = sevicesNode.Item(0);
                ssp.ServiceName = PubFun.GetXmlNodeAttributeValue(saas, "name", "");
                ssp.Namespace = doc.SelectSingleNode("//@targetNamespace").Value;
                //var ttn = doc.SelectSingleNode("//@targetNamespace");
                //var soapaddress = doc.DocumentElement.GetElementsByTagName("soap:address ");
                foreach (XmlNode nade in saas.ChildNodes)
                {
                    foreach (XmlNode nade1 in nade.ChildNodes)
                    {
                        if (nade1.Name.Contains("address"))
                        {
                            ssp.Address = PubFun.GetXmlNodeAttributeValue(nade1, "location");
                            break;
                        }
                    }
                }
            }
            #endregion
            #region ComplexTypes
            #region complexTypeFuc
            Action<ServiceSoap, XmlNode, XmlNode, bool> complexTypeFuc = (_ssp, pnade, snade, iscomplexType) =>
            {
                if (pnade.HasChildNodes && snade.HasChildNodes)
                {
                    foreach (XmlNode nade2 in snade.ChildNodes)
                    {
                        var complexType = new OperationInputOutput();
                        complexType.isComplexType = iscomplexType;
                        complexType.elementName = PubFun.GetXmlNodeAttributeValue(pnade, "name");
                        complexType.name = PubFun.GetXmlNodeAttributeValue(nade2, "name");
                        complexType.type = PubFun.GetXmlNodeAttributeValue(nade2, "type");
                        complexType.nillable = PubFun.GetXmlNodeAttributeValue(nade2, "nillable", "true").ChanageType<bool>();
                        complexType.minOccurs = PubFun.GetXmlNodeAttributeValue(nade2, "minOccurs", "0");
                        complexType.maxOccurs = PubFun.GetXmlNodeAttributeValue(nade2, "maxOccurs", "1");

                        foreach (XmlAttribute xab in nade2.Attributes)
                        {
                            if (xab?.Prefix?.Equals("xmlns", StringComparison.OrdinalIgnoreCase) == true
                                || xab?.Name?.Equals("xmlns", StringComparison.OrdinalIgnoreCase) == true)
                            {
                                complexType.xmlns = xab.Value;
                                break;
                            }
                        }
                        _ssp.ComplexTypes.Add(complexType);
                    }
                }
            };
            #endregion
            ssp.ComplexTypes = new List<OperationInputOutput>();
            var wsdlTypes = doc.DocumentElement.GetElementsByTagName("wsdl:types");
            if (wsdlTypes == null || wsdlTypes.Count == 0)
                wsdlTypes = doc.DocumentElement.GetElementsByTagName("types");
            if (wsdlTypes?.Count > 0)
            {
                var wsdlType = wsdlTypes.Item(0);
                foreach (XmlNode nade in wsdlType.ChildNodes)
                {
                    foreach (XmlNode nade1 in nade.ChildNodes)
                    {
                        #region s:complexType
                        if (nade1.Name.Equals("s:complexType", StringComparison.OrdinalIgnoreCase)
                            || nade1.Name.Equals("xs:complexType", StringComparison.OrdinalIgnoreCase)
                            || nade1.Name.Equals("complexType", StringComparison.OrdinalIgnoreCase))
                        {
                            if (nade1.HasChildNodes && nade1.ChildNodes[0].HasChildNodes)
                            {
                                complexTypeFuc(ssp, nade1, nade1.ChildNodes[0], true);
                            }
                        }
                        #endregion
                        #region s:element
                        else if (nade1.Name.Equals("s:element", StringComparison.OrdinalIgnoreCase)
                            || nade1.Name.Equals("xs:element", StringComparison.OrdinalIgnoreCase)
                            || nade1.Name.Equals("element", StringComparison.OrdinalIgnoreCase))
                        {
                            if (nade1.HasChildNodes && nade1.ChildNodes[0].HasChildNodes && nade1.ChildNodes[0].ChildNodes[0].HasChildNodes)
                            {
                                complexTypeFuc(ssp, nade1, nade1.ChildNodes[0].ChildNodes[0], false);
                            }
                        }
                        #endregion

                        #region xsd:import
                        else if (nade1.Name.Equals("xsd:import", StringComparison.OrdinalIgnoreCase))
                        {
                            var schemaLocation = PubFun.GetXmlNodeAttributeValue(nade1, "schemaLocation");
                            if (!string.IsNullOrWhiteSpace(schemaLocation))
                            {
                                var xsdDoc = loadXmlDocument(schemaLocation);

                                //var schemas = xsdDoc.DocumentElement.GetElementsByTagName("xs:schema");
                                //if (schemas == null || schemas.Count == 0)
                                //    schemas = xsdDoc.DocumentElement.GetElementsByTagName("schema");
                                if (xsdDoc?.HasChildNodes==true && xsdDoc.ChildNodes?.Count > 0)
                                {
                                    //var schema = schemas.Item(0);
                                    foreach (XmlNode xsnade in xsdDoc.ChildNodes)
                                    {
                                        foreach (XmlNode xsnade1 in xsnade.ChildNodes)
                                        {
                                            #region s:complexType
                                            if (xsnade1.Name.Equals("s:complexType", StringComparison.OrdinalIgnoreCase)
                                                || xsnade1.Name.Equals("xs:complexType", StringComparison.OrdinalIgnoreCase)
                                                || xsnade1.Name.Equals("complexType", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (xsnade1.HasChildNodes && xsnade1.ChildNodes[0].HasChildNodes)
                                                {
                                                    complexTypeFuc(ssp, xsnade1, xsnade1.ChildNodes[0], true);
                                                }
                                            }
                                            #endregion
                                            #region s:element
                                            else if (xsnade1.Name.Equals("s:element", StringComparison.OrdinalIgnoreCase)
                                                || xsnade1.Name.Equals("xs:element", StringComparison.OrdinalIgnoreCase)
                                                || xsnade1.Name.Equals("element", StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (xsnade1.HasChildNodes && xsnade1.ChildNodes[0].HasChildNodes && xsnade1.ChildNodes[0].ChildNodes[0].HasChildNodes)
                                                {
                                                    complexTypeFuc(ssp, xsnade1, xsnade1.ChildNodes[0].ChildNodes[0], false);
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                    }
                }
                ssp.ComplexTypes.ForEach(ct =>
                {
                    ct.tnsTypes = ssp.ComplexTypes.FindAll(f => f.elementName.Equals(ct.tnsType, StringComparison.OrdinalIgnoreCase));
                });
            }
            #endregion

            #region Operations
            ssp.Operations = new List<ServiceOperation>();
            ssp.DicOperations = new Dictionary<string, ServiceOperation>(StringComparer.OrdinalIgnoreCase);
            var wsdlbindings = doc.DocumentElement.GetElementsByTagName("wsdl:binding");
            if (wsdlbindings == null || wsdlbindings.Count == 0)
                wsdlbindings = doc.DocumentElement.GetElementsByTagName("binding");
            if (wsdlbindings?.Count > 0)
            {
                var wsdlbinding = wsdlbindings.Item(0);
                foreach (XmlNode nade in wsdlbinding.ChildNodes)
                {
                    if (nade.Name.Equals("wsdl:operation", StringComparison.OrdinalIgnoreCase)
                        || nade.Name.Equals("operation", StringComparison.OrdinalIgnoreCase))
                    {
                        var operations = new ServiceOperation();
                        operations.Name = PubFun.GetXmlNodeAttributeValue(nade, "name");
                        foreach (XmlNode nade1 in nade.ChildNodes)
                        {
                            if (nade1.Name.Equals("soap:operation", StringComparison.OrdinalIgnoreCase)
                                || nade1.Name.Equals("operation", StringComparison.OrdinalIgnoreCase))
                            {
                                operations.SoapAction = PubFun.GetXmlNodeAttributeValue(nade1, "soapAction", "http://tempuri.org/" + operations.Name);
                                break;
                            }
                        }
                        ssp.Operations.Add(operations);
                    }
                }

                ssp.Operations.ForEach(ct =>
                {
                    ct.Inputs = ssp.ComplexTypes.FindAll(f => f.isComplexType == false && f.elementName.Equals(ct.Name, StringComparison.OrdinalIgnoreCase));
                    ct.Output = ssp.ComplexTypes.Find(f => f.isComplexType == false && f.elementName.Equals(ct.Name + "Response", StringComparison.OrdinalIgnoreCase));

                    ssp.DicOperations[ct.Name] = ct;
                });
            }
            #endregion

            //DIC_ServiceSoap[url] = ssp;
            //DIC_ServiceSoap[getUrl(url)] = ssp;

            return ssp;
        }

        #region ServiceEntity
        /// <summary>
        /// ������Ϣ
        /// </summary>
        class ServiceSoap
        {
            public string Namespace { get; set; }
            public string ServiceName { get; set; }
            public string Address { get; set; }
            public List<ServiceOperation> Operations { get; set; }
            public Dictionary<string, ServiceOperation> DicOperations { get; set; }
            public List<OperationInputOutput> ComplexTypes { get; set; }
            public override string ToString()
            {
                return $"ServiceName={ServiceName},Address={Address},Operations.Count={Operations?.Count},ComplexTypes.Count = { ComplexTypes?.Count}";
            }
        }
        /// <summary>
        /// ������Ϣ
        /// </summary>
        class ServiceOperation
        {
            public string Name { get; set; }
            public string SoapAction { get; set; }
            public List<OperationInputOutput> Inputs { get; set; }
            public OperationInputOutput Output { get; set; }

            public override string ToString()
            {
                return $"Name={Name},SoapAction={SoapAction},Inputs.Count={Inputs?.Count},Output = { Output}";
            }
        }

        class OperationInputOutput
        {
            public string elementName { get; set; }
            /// <summary>
            /// �涨Ԫ�ص����ơ������Ԫ���� schema Ԫ�أ���������Ǳ���ġ�
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// �涨�ڽ��������͵����ƣ����߹涨 simpleType �� complexType Ԫ�ص����ơ�
            /// </summary>
            public string type { get; set; }
            public string tnsType
            {
                get
                {
                    if (!string.IsNullOrWhiteSpace(type))
                    {
                        var types = type.Split(':');
                        return types.Length > 1 ? types[1] : "";
                    }
                    else return "";
                }
            }
            /// <summary>
            /// �涨 element Ԫ���ڸ�Ԫ���пɳ��ֵ���С��������ֵ�����Ǵ��ڻ�������������Ĭ��ֵΪ 1�� 
            /// �����Ԫ���� schema Ԫ�أ�����ʹ�ø����ԡ�
            /// </summary>
            public string minOccurs { get; set; }
            /// <summary>
            /// �涨 element Ԫ���ڸ�Ԫ���пɳ��ֵ�����������ֵ�����Ǵ��ڻ�������������
            /// ������������������κ����ƣ���ʹ���ַ��� "unbounded"�� Ĭ��ֵΪ 1��
            /// </summary>
            public string maxOccurs { get; set; }
            /// <summary>
            /// �Ƿ�����򼯺�
            /// </summary>
            public bool isArrayOf
            {
                get
                {
                    return maxOccurs?.Equals("unbounded", StringComparison.OrdinalIgnoreCase) == true
                        || type?.ToLower().Contains("Array".ToLower()) == true;
                }
            }
            public bool nillable { get; set; } = true;

            bool _isComplexType = false;
            /// <summary>
            /// �Ƿ�������
            /// </summary>
            public bool isComplexType
            {
                get
                {
                    return _isComplexType;// || tnsTypes?.Count > 0;
                }
                set { _isComplexType = value; }
            }
            /// <summary>
            /// ������
            /// </summary>
            public List<OperationInputOutput> tnsTypes { get; set; }
            public string xmlns { get; set; }
            public override string ToString()
            {
                return $"elementName={elementName},Name={name},type={type},isArrayOf={isArrayOf},isComplexType = {isComplexType}";
            }
        }
        #endregion
        #endregion
    }
}