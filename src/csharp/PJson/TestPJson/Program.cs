using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PJson; 

namespace TestPJson
{
    public class Program
    {
        public static void Main()
        {
            Test1();
            Test2();

            TestWriter.Test();
        }

        public static void Test1()
        {
            string pjson = """
[
    {
        name:"AAA.BBB.InterfaceService_to_YingJiBu",
        action:"restart",
        "注释1":"86400 = 1天",
        check_duration_secs:86400,
    },
    {
        b:{
            c: -9.15,
            z:[
                "as\n\\df",
                3.12,
                true,
                {a:-8}
            ]
        },
        "asdf": true,
        a:1
    },
    {
        "asdf": -3.67,
        ppp:{
            ddd:{
                a:"d",
            },
        },
    },
]
""";

            string jsonString = PJsonReader.FromPJson(pjson);
            string result = jsonString;

            string expectedJson = """
[
    {
        "name":"AAA.BBB.InterfaceService_to_YingJiBu",
        "action":"restart",
        "注释1":"86400 = 1天",
        "check_duration_secs":86400
    },
    {
        "b":{
            "c": -9.15,
            "z":[
                "as\n\\df",
                3.12,
                true,
                {"a":-8}
            ]
        },
        "asdf": true,
        "a":1
    },
    {
        "asdf": -3.67,
        "ppp":{
            "ddd":{
                "a":"d"
            }
        }
    }
]
""";

            Console.WriteLine("Test1 Result:");
            Console.WriteLine(result);
            System.Diagnostics.Debug.Assert(expectedJson == result);
        }

        public static void Test2()
        {
            string pjson = """
    //a 
[
    //单行注释 " \ ` '  注释
    {
        "name":`'`AAA.BBB.InterfaceService_to_YingJiBu"asdfasdf`'`,//单行注释 " \ ` '  注释
        //单行注释 " \ ` '  注释
        action:"restart",
        `''`注释1`''`: `''`86400 = "1天"`''`,
        check_duration_secs:86400,
    },
    {
        b:{
            c: -9.15,
            z:[
                "as\n\\df", 3.12, true, {a:-8},
                //单行注释 " \ ` '  注释
`''''`
[
    //单行注释 " \  '  注释
    {
        "name":'AAA.BBB.InterfaceService_to_YingJiBu"asdfasdf',//单行注释 " \  '  注释
        //单行注释 " \  '  注释
        "action":"restart",
        ''注释1'': ''86400 = "1天"'',
        "check_duration_secs":86400,
    },
    {
        "b":{
            "c": -9.15,
            "z":[
                "as\n\\df", 3.12, true, {"a":-8},
                //单行注释 " \  '  注释
tag

tag ,
            ]
        },
        "asdf": true ,
    },
    {
        "asdf": -3.67,
    },
]
`''''` ,
            ]
        },
        asdf: true ,
    },
    {
        asdf: -3.67,
    },//comment
]
""";

            string jsonString = PJsonReader.FromPJson(pjson);
            string result = jsonString;

            string expectedJson = """
[
  {
    "action": "restart",
    "check_duration_secs": 86400,
    "name": "AAA.BBB.InterfaceService_to_YingJiBu\"asdfasdf",
    "注释1": "86400 = \"1天\""
  },
  {
    "asdf": true,
    "b": {
      "c": -9.15,
      "z": [
        "as\n\\df",
        3.12,
        true,
        {
          "a": -8
        },
        "\n[\n    //单行注释 \" \\  '  注释\n    {\n        \"name\":'AAA.BBB.InterfaceService_to_YingJiBu\"asdfasdf',//单行注释 \" \\  '  注释\n        //单行注释 \" \\  '  注释\n        \"action\":\"restart\",\n        ''注释1'': ''86400 = \"1天\"'',\n        \"check_duration_secs\":86400,\n    },\n    {\n        \"b\":{\n            \"c\": -9.15,\n            \"z\":[\n                \"as\\n\\\\df\", 3.12, true, {\"a\":-8},\n                //单行注释 \" \\  '  注释\ntag\n\ntag ,\n            ]\n        },\n        \"asdf\": true ,\n    },\n    {\n        \"asdf\": -3.67,\n    },\n]\n"
      ]
    }
  },
  {
    "asdf": -3.67
  }
]
""";

            Console.WriteLine("Test2 Result:");
            Console.WriteLine(result);

            var expectedJsonObject = System.Text.Json.Nodes.JsonNode.Parse(expectedJson);
            var resultJsonObject = System.Text.Json.Nodes.JsonArray.Parse(result);

            var expectedJson2 = resultJsonObject.ToJsonString();
            var result2 = resultJsonObject.ToJsonString();

            System.Diagnostics.Debug.Assert(expectedJson2 == result2);
        }
    }
}
