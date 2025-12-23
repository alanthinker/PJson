#[cfg(test)]
mod tests {

    use pjson::PJsonReader;
    use serde_json;

    #[test]
    fn test1() {
        let pjson = r##"
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
"##;

        let json_bytes = PJsonReader::from_pjson(pjson.as_bytes());
        let result = String::from_utf8(json_bytes).unwrap();

        let json = r##"
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
"##;

        println!("{}", result);
        assert_eq!(json, result);
    }

    #[test]
    fn test2() {
        let pjson = r#####"
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
"#####;

        let json_bytes = PJsonReader::from_pjson(pjson.as_bytes());

        let json = r#####"
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
"#####;

        let ob_json: serde_json::Value = serde_json::de::from_str(&json).unwrap();
        let r2_json = serde_json::ser::to_string_pretty(&ob_json).unwrap();

        println!("json_bytes:\n{}", String::from_utf8_lossy(&json_bytes));

        let ob_pjson: serde_json::Value = serde_json::de::from_slice(&json_bytes).unwrap();
        let r2_pjson = serde_json::ser::to_string_pretty(&ob_pjson).unwrap();

        println!("r2_pjson:\n{}", r2_pjson);
        assert_eq!(r2_json, r2_pjson);

        println!(
            "ob_pjson data:\n{}",
            ob_pjson[1]["b"]["z"][4].as_str().unwrap()
        );
        assert_eq!(ob_json[1]["b"]["z"][4], ob_pjson[1]["b"]["z"][4]);
    }
}
