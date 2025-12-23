using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PJson
{
    public class PJsonWriter
    {
        private const string DefaultTag = "";
        private const char TagChar = '`';

        public static string ToPJson(string jsonString)
        {
            var jsonNode = JsonNode.Parse(jsonString);
            var sb = new StringBuilder();
            var context = new WriterContext();

            WriteNode(jsonNode, sb, context);

            return sb.ToString();
        }

        private static void WriteNode(JsonNode node, StringBuilder sb, WriterContext context)
        {
            switch (node)
            {
                case JsonObject obj:
                    WriteObject(obj, sb, context);
                    break;
                case JsonArray array:
                    WriteArray(array, sb, context);
                    break;
                case JsonValue value:
                    WriteValue(value, sb, context);
                    break;
            }
        }

        private static void WriteObject(JsonObject obj, StringBuilder sb, WriterContext context)
        {
            sb.Append('{');

            var properties = obj.ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                // 写入属性名（带双引号）
                sb.Append('\n');
                sb.Append(context.Indent);
                sb.Append('"');
                sb.Append(property.Key);
                sb.Append('"');
                sb.Append(':');
                sb.Append(' ');

                // 写入属性值
                var nestedContext = context.Enter();
                WriteNode(property.Value, sb, nestedContext);

                // 最后一个属性也加逗号
                if (i < properties.Count - 1)
                {
                    sb.Append(',');
                }
                else
                {
                    sb.Append(',');
                }
            }

            if (properties.Count > 0)
            {
                sb.Append('\n');
                sb.Append(context.ParentIndent);
            }
            sb.Append('}');
        }

        private static void WriteArray(JsonArray array, StringBuilder sb, WriterContext context)
        {
            sb.Append('[');

            var elements = array.ToList();
            if (elements.Count > 0 && ShouldUseMultiline(elements))
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];

                    sb.Append('\n');
                    sb.Append(context.Indent);

                    WriteNode(element, sb, context);

                    // 最后一个元素也加逗号
                    if (i < elements.Count - 1)
                    {
                        sb.Append(',');
                    }
                    else
                    {
                        sb.Append(',');
                    }
                }

                sb.Append('\n');
                sb.Append(context.ParentIndent);
            }
            else
            {
                // 简单数组，单行显示
                for (int i = 0; i < elements.Count; i++)
                {
                    var element = elements[i];
                    WriteNode(element, sb, context);

                    if (i < elements.Count - 1)
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        sb.Append(',');
                    }
                }
            }

            sb.Append(']');
        }

        private static bool ShouldUseMultiline(List<JsonNode> elements)
        {
            // 如果数组包含对象或数组，使用多行显示
            return elements.Any(e => e is JsonObject || e is JsonArray);
        }

        private static void WriteValue(JsonValue value, StringBuilder sb, WriterContext context)
        {
            if (value.TryGetValue<JsonElement>(out var element))
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        WriteString(element.GetString(), sb, context);
                        break;
                    case JsonValueKind.Number:
                        sb.Append(element.GetRawText());
                        break;
                    case JsonValueKind.True:
                        sb.Append("true");
                        break;
                    case JsonValueKind.False:
                        sb.Append("false");
                        break;
                    case JsonValueKind.Null:
                        sb.Append("null");
                        break;
                }
            }
            else if (value.TryGetValue<string>(out var str))
            {
                WriteString(str, sb, context);
            }
            else if (value.TryGetValue<double>(out var number))
            {
                sb.Append(number.ToString());
            }
            else if (value.TryGetValue<bool>(out var boolean))
            {
                sb.Append(boolean ? "true" : "false");
            }
            else if (value == null)
            {
                sb.Append("null");
            }
        }

        private static void WriteString(string str, StringBuilder sb, WriterContext context)
        {
            // 字符串值用广义字符串包裹
            var tag = GenerateTag(str, context);

            sb.Append(TagChar);
            sb.Append(tag);
            sb.Append(TagChar);

            // 写入字符串内容（不转义）
            sb.Append(str);

            sb.Append(TagChar);
            sb.Append(tag);
            sb.Append(TagChar);
        }

        private static string GenerateTag(string content, WriterContext context)
        {
            // 尝试找到不包含在内容中的标签
            var tag = DefaultTag;
            var attempts = 0;

            while (ContainsTagInContent(content, tag))
            {
                attempts++;
                tag = new string('_', attempts);
            }

            return tag;
        }

        private static bool ContainsTagInContent(string content, string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                // 空标签，检查是否有单独的 ` 字符
                return content.Contains(TagChar);
            }

            // 检查内容是否包含标签边界
            var startTag = $"{TagChar}{tag}{TagChar}";
            var endTag = $"{TagChar}{tag}{TagChar}";

            return content.Contains(startTag) || content.Contains(endTag);
        }

        private class WriterContext
        {
            private int depth = 0;
            private const int SpacesPerIndent = 2; // 改为2空格缩进

            public string Indent => new string(' ', depth * SpacesPerIndent);
            public string ParentIndent => depth > 0 ? new string(' ', (depth - 1) * SpacesPerIndent) : "";

            public WriterContext Enter()
            {
                return new WriterContext { depth = this.depth + 1 };
            }
        }
    }
}