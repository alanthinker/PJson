# 说明

我自己做了个特殊json格式.
支持注释, 支持最后一个元素后的逗号, 属性名可以不用双引号,
最关键的是支持高级字符串. 特别适合做配置文件使用.
比如下面及格例子:


```pjson
{
    Name: "字符串",

    Content1: ``字符串``,

    Content2: `_`字符串: 有引号 " ` 和 \ / 也没关系 `_`,

    Content3: `tag`字符串: 有 ` 符号也没关系 `tag`,

    Content4: `tag2`字符串: 有其他 `tag` 也没关系 `tag2`,

    // 支持注释
    Content5: ``字符串: 有换行符更没关系
其他内容1
其他内容2
``, 
}
```

这对配置文件非常友好, 字符串内完全避免了使用转义字符.
如果字符串的开头是 `your_tag`, 那么结尾也是 'your_tag' 即可

# 项目说明
* 此项目包含 csharp 和 rust 实现的 pjson reader.
  可以把 pjson 转换为普通 json, 方便程序读取.
* 此项目包含了 vs code 插件, 对 pjson 进行语法高亮.
* 如果需要完整的读写功能和序列化反序列化功能, 请使用我开发的另外一个项目
  https://github.com/alanthinker/Newtonsoft.Json

# vscode 效果图:
<img src="https://github.com/alanthinker/PJson/blob/master/example.png" />
