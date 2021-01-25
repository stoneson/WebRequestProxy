# WebRequestProxy
一个通用的Web Api,Webservice,WCF调用类库


测试用例：

服务代码：服务端代码可参考：https://github.com/stoneson/WebServiceTest

        [WebMethod]
        public WeatherForecast getBySummarie(string summarie)
        {
            var rng = new Random();
            var ls = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                code = index,
                msg = "成功" + index,
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
             .ToList();
            var fd = ls.FirstOrDefault(f => f.Summary == summarie);
            if (fd == null) fd = ls.FirstOrDefault();
            return fd;
        }
        
         [WebMethod]
        public string AddCar(MyCar myCar)
        {
            var _car = MyCar;

            return ResponseResultObj.Success(_car, "Success").ToJson();
        }
        
         [WebMethod]
        public string AddCars(List<MyCar> cars)
        {
            var _car = cars;

            return ResponseResultObj.Success(_car, "Success").ToJson();
        }
        
调用示例：
  
  //构造soap请求信息
	//服务地址
	var url = "http://localhost:44351/WebService1.asmx"; 
		WCF用："http://localhost:44351/ServiceTest.svc";
  
	//调用方法名
	const string methodName = "getBySummarie";
  
	//构建参数对象
	var hab = new Dictionary<string, object>();
	hab.Add("summarie", "test");

	//通过Soap调用,返回对象数据
	var result = WebRequestProxy.WebServiceCaller.Query(url, methodName, hab);
	
	//或者：
	
	//构建JSON对象或实体	
	var jsonStr = {  "Summarie": "test"};

	//通过Soap调用,返回对象数据	
	var result = WebRequestProxy.WebServiceCaller.Query(url, methodName, jsonStr);
            
  //-----------------------------------------------------------------------------------------------------
  
  //调用方法名
  
	const string methodName = "AddCar";
	//构建JSON对象或实体
	
	var jsonStr = {"myCar":{"id":"44","name":"423","mycode2":"42","code1":2,"msg1":"42","wfList":[{"code":"1","msg":"成功1","Date":"2021-01-23T21:14:01.0831132+08:00","TemperatureC":"6","Summary":"Hot","myCar":{"id":"424","name":"4323","mycode2":"42","code1":2},"MyCar2List":[{"id":"1","name":"4323","mycode2":"42"}]}],"wf":{"code":"4","msg":"成功4","Date":"2021-01-25T21:14:01.0841135+08:00","TemperatureC":"10","Summary":"Hot4","myCar":{"id":"424","name":"4323","mycode2":"42","code1":2},"MyCar2List":[{"id":"4","name":"4323","mycode2":"42"}]}}};
	
	//通过Soap调用,返回对象数据
	
	var result = WebRequestProxy.WebServiceCaller.Query(url, methodName, jsonStr);
  
  //--------------------------------------------------------------------------------
  
  //调用方法名
  
	const string methodName = "AddCar";
	
	//构建JSON对象或实体
	
	var jsonStr = {"cars":[{"myCar":{"id":"23","name":"423","mycode2":"42","code1":2,"msg1":"42","wfList":[{"code":"1","msg":"成功1","Date":"2021-01-23T21:14:01.0831132+08:00","TemperatureC":"6","Summary":"Hot","myCar":{"id":"424","name":"4323","mycode2":"42","code1":2},"MyCar2List":[{"id":"1","name":"4323","mycode2":"42"}]}],"wf":{"code":"4","msg":"成功4","Date":"2021-01-25T21:14:01.0841135+08:00","TemperatureC":"10","Summary":"Hot4","myCar":{"id":"424","name":"4323","mycode2":"42","code1":2},"MyCar2List":[{"id":"4","name":"4323","mycode2":"42"}]}}}，{"myCar":{"id":"12","name":"423","mycode2":"42","code1":2,"msg1":"42","wfList":[{"code":"1","msg":"成功1","Date":"2021-01-23T21:14:01.0831132+08:00","TemperatureC":"6","Summary":"Hot","myCar":{"id":"424","name":"4323","mycode2":"42","code1":2},"MyCar2List":[{"id":"1","name":"4323","mycode2":"42"}]}],"wf":{"code":"4","msg":"成功4","Date":"2021-01-25T21:14:01.0841135+08:00","TemperatureC":"10","Summary":"Hot4","myCar":{"id":"424","name":"4323","mycode2":"42","code1":2},"MyCar2List":[{"id":"4","name":"4323","mycode2":"42"}]}}}]};

	//通过Soap调用,返回对象数据
	
	var result = WebRequestProxy.WebServiceCaller.Query(url, methodName, jsonStr);
 
  
