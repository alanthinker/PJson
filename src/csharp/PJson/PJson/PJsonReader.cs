using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PJson;

public enum JsonState
{
    None,
    InArray,
    PsKey,
    PsValue,
    InString,
    InEscape,
    InOneLineComment,
    InGAStringStart,
    InGAStringEnd,
    InGAString
}

public class PJsonReader
{
    private string pjson;
    private int index;
    private StringBuilder outBuffer;
    private Stack<JsonState> states;
    private int GAStartIndex;
    private string GATag;
    private StringBuilder GATempStringBuffer;

    public PJsonReader(string pjsonString)
    {
        pjson = pjsonString;
        index = 0;
        outBuffer = new StringBuilder();
        states = new Stack<JsonState>();
        GAStartIndex = 0;
        GATag = "";
        GATempStringBuffer = new StringBuilder();
    }

    public static string FromPJson(string pjsonString)
    {
        var pjsonReader = new PJsonReader(pjsonString);

        // 去除 UTF8 BOM 头: efbbbf, 防止 serde json 无法解析json.
        if (pjsonReader.pjson.Length > 3 && pjsonReader.pjson[0] == '\u00ef' && pjsonReader.pjson[1] == '\u00bb' && pjsonReader.pjson[2] == '\u00bf')
        {
            pjsonReader.index += 3;
        }

        pjsonReader.states.Push(JsonState.None);

        while (pjsonReader.index < pjsonReader.pjson.Length)
        {
            var state = pjsonReader.states.Peek();
            switch (state)
            {
                case JsonState.None:
                    pjsonReader.ProcessStateNone();
                    break;
                case JsonState.InArray:
                    pjsonReader.ProcessStateInArray();
                    break;
                case JsonState.PsKey:
                    pjsonReader.ProcessStatePsKey();
                    break;
                case JsonState.PsValue:
                    pjsonReader.ProcessStatePsValue();
                    break;
                case JsonState.InString:
                    pjsonReader.ProcessStateInString();
                    break;
                case JsonState.InEscape:
                    pjsonReader.ProcessStateInEscape();
                    break;
                case JsonState.InGAStringStart:
                    pjsonReader.ProcessStateInGAStringStart();
                    break;
                case JsonState.InGAStringEnd:
                    pjsonReader.ProcessStateInGAStringEnd();
                    break;
                case JsonState.InGAString:
                    pjsonReader.ProcessStateInGAString();
                    break;
                case JsonState.InOneLineComment:
                    pjsonReader.ProcessStateInOneLineComment();
                    break;
            }

            pjsonReader.index++;
        }

        return pjsonReader.outBuffer.ToString();
    }

    private void ProcessStateNone()
    {
        char ch = pjson[index];
        switch (ch)
        {
            case '{':
                outBuffer.Append(ch);
                states.Push(JsonState.PsKey);
                break;
            case '[':
                states.Push(JsonState.InArray);
                outBuffer.Append(ch);
                break;
            case '/':
                if (pjson[index + 1] == '/')
                {
                    states.Push(JsonState.InOneLineComment);
                }
                break;
            default:
                outBuffer.Append(ch);
                break;
        }
    }

    private void ProcessStateInArray()
    {
        char ch = pjson[index];
        switch (ch)
        {
            case '"':
                states.Push(JsonState.InString);
                outBuffer.Append(ch);
                break;
            case '`':
                states.Push(JsonState.InGAStringStart);
                GAStartIndex = index;
                outBuffer.Append('"');
                break;
            case '{':
                outBuffer.Append(ch);
                states.Push(JsonState.PsKey);
                break;
            case '[':
                states.Push(JsonState.InArray);
                outBuffer.Append(ch);
                break;
            case ']':
                states.Pop();
                EatExComma();
                outBuffer.Append(ch);
                break;
            case '/':
                if (pjson[index + 1] == '/')
                {
                    states.Push(JsonState.InOneLineComment);
                }
                break;
            default:
                outBuffer.Append(ch);
                break;
        }
    }

    private void ProcessStatePsKey()
    {
        char ch = pjson[index];
        switch (ch)
        {
            case '"':
                states.Push(JsonState.InString);
                outBuffer.Append(ch);
                break;
            case '`':
                states.Push(JsonState.InGAStringStart);
                GAStartIndex = index;
                outBuffer.Append('"');
                break;
            case ':':
                states.Pop();
                states.Push(JsonState.PsValue);
                outBuffer.Append(ch);
                break;
            case '/':
                if (pjson[index + 1] == '/')
                {
                    states.Push(JsonState.InOneLineComment);
                }
                break;
            case '}':
                states.Pop();
                EatExComma();
                outBuffer.Append(ch);
                break;
            case ']':
                states.Pop();
                EatExComma();
                outBuffer.Append(ch);
                break;
            default:
                if (IsAsciiGraphic(ch))
                {
                    ProcessNoQuotationKey();
                }
                else
                {
                    outBuffer.Append(ch);
                }
                break;
        }
    }

    private void ProcessNoQuotationKey()
    {
        outBuffer.Append('"');
        while (pjson[index] != ':' && IsAsciiGraphic(pjson[index]))
        {
            outBuffer.Append(pjson[index]);
            index++;
        }
        outBuffer.Append('"');
        index--;
    }

    private void ProcessStatePsValue()
    {
        char ch = pjson[index];
        switch (ch)
        {
            case '"':
                states.Push(JsonState.InString);
                outBuffer.Append(ch);
                break;
            case '`':
                states.Push(JsonState.InGAStringStart);
                GAStartIndex = index;
                outBuffer.Append('"');
                break;
            case '{':
                outBuffer.Append(ch);
                states.Push(JsonState.PsKey);
                break;
            case '[':
                states.Push(JsonState.InArray);
                outBuffer.Append(ch);
                break;
            case ',':
                states.Pop();
                states.Push(JsonState.PsKey);
                outBuffer.Append(ch);
                break;
            case '}':
                states.Pop();
                outBuffer.Append(ch);
                break;
            case ']':
                // 不会进入这里, 因为 PsValue 是属于 object 的
                break;
            case '/':
                if (pjson[index + 1] == '/')
                {
                    states.Push(JsonState.InOneLineComment);
                }
                break;
            default:
                outBuffer.Append(ch);
                break;
        }
    }

    private void ProcessStateInEscape()
    {
        char ch = pjson[index];
        states.Pop();
        outBuffer.Append(ch);
    }

    private void ProcessStateInOneLineComment()
    {
        char ch = pjson[index];
        if (ch == '\n')
        {
            states.Pop();
            outBuffer.Append(ch);
        }
    }

    private void ProcessStateInString()
    {
        char ch = pjson[index];
        switch (ch)
        {
            case '"':
                states.Pop();
                outBuffer.Append(ch);
                break;
            case '\\':
                states.Push(JsonState.InEscape);
                outBuffer.Append(ch);
                break;
            default:
                outBuffer.Append(ch);
                break;
        }
    }

    private void ProcessStateInGAStringStart()
    {
        char ch = pjson[index];
        if (ch == '`')
        {
            states.Pop();
            states.Push(JsonState.InGAString);
            GATag = pjson.Substring(GAStartIndex + 1, index - (GAStartIndex + 1));
        }
    }

    private void ProcessStateInGAStringEnd()
    {
        char ch = pjson[index];
        if (ch == '`')
        {
            states.Pop();

            string content = GATempStringBuffer.ToString();
            content = content.Replace("\\", @"\\");
            content = content.Replace("\r", "");
            content = content.Replace("\n", @"\n");
            content = content.Replace("\"", "\\\"");

            outBuffer.Append(content);
            GATempStringBuffer.Clear();
            outBuffer.Append('"');
        }
    }

    private void ProcessStateInGAString()
    {
        char ch = pjson[index];
        if (ch == '`')
        {
            if (pjson[index + 1 + GATag.Length] == '`' && pjson.Substring(index + 1, GATag.Length) == GATag)
            {
                states.Pop();
                states.Push(JsonState.InGAStringEnd);
            }
        }
        else
        {
            GATempStringBuffer.Append(ch);
        }
    }

    private void EatExComma()
    {
        int p = outBuffer.Length - 1;
        while (!IsAsciiGraphic(outBuffer[p]))
        {
            p--;
        }
        if (outBuffer[p] == ',')
        {
            outBuffer.Remove(p, 1);
        }
    }

    private bool IsAsciiGraphic(char ch)
    {
        return ch >= 0x21 && ch <= 0x7E;
    }
}

