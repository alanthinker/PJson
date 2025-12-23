#![allow(unused_variables)]
#![allow(non_snake_case)]

use std::collections::VecDeque;

#[derive(PartialEq, Clone, Copy, Debug)]
enum JsonState {
    None,
    InArray,
    //position for object key
    PsKey,
    //position for object value
    PsValue,
    InString,
    InEscape,

    InOneLineComment,

    //GraveAccent:
    InGAStringStart,
    InGAStringEnd,
    InGAString,
}

pub struct PJsonReader<'a> {
    pjson: &'a [u8],
    index: usize,
    out_buffer: Vec<u8>,
    states: VecDeque<JsonState>,
    GA_start_index: usize,
    GA_Tag: String,
    GA_temp_string_buffer: Vec<u8>,
}

// 注意:
// * 假设输入文本编码为 utf-8
// * 不检查输入文本 pjson 格式的合法性. 比如 key 和 value 等都是直接复制原始 pjson 到目标 json 的
// * 允许单行注释
// * 允许对象和数组的最后一个元素后面有逗号
// * 只读取pjson, 不保存pjson, 因为pjson只用于配置文件. 如果要保存动态数据, 另存到另外一个json文件中.
//     下次启动的时候, 再合并2个文件的静态的数据和动态的数据, 好处是: 不会丢失注释, 不会因为保存异常丢失静态配置数据.

impl<'a> PJsonReader<'a> {
    pub fn from_pjson(pjson_bytes: &[u8]) -> Vec<u8> {
        let mut pjson_reader = PJsonReader {
            pjson: pjson_bytes,
            index: 0,
            out_buffer: vec![],
            states: VecDeque::<JsonState>::new(),
            GA_start_index: 0,
            GA_Tag: "".to_owned(),
            GA_temp_string_buffer: vec![],
        };

        // 去除 utf8 BOM 头: efbbbf, 防止 serde json 无法解析json.
        if pjson_reader.pjson.len() > 3
            && pjson_reader.pjson[0] == 0xef
            && pjson_reader.pjson[1] == 0xbb
            && pjson_reader.pjson[2] == 0xbf
        {
            pjson_reader.index += 3;
        }

        pjson_reader.states.push_back(JsonState::None);

        while pjson_reader.index < pjson_reader.pjson.len() {
            let state_opt = pjson_reader.states.back();
            match state_opt {
                Some(state) => match state {
                    JsonState::None => {
                        pjson_reader.process_state_None();
                    }
                    JsonState::InArray => {
                        pjson_reader.process_state_InArray();
                    }
                    JsonState::PsKey => {
                        pjson_reader.process_state_PsKey();
                    }
                    JsonState::PsValue => {
                        pjson_reader.process_state_PsValue();
                    }
                    JsonState::InString => {
                        pjson_reader.process_state_InString();
                    }
                    JsonState::InEscape => {
                        pjson_reader.process_state_InEscape();
                    }
                    JsonState::InGAStringStart => {
                        pjson_reader.process_state_InGAStringStart();
                    }
                    JsonState::InGAStringEnd => {
                        pjson_reader.process_state_InGAStringEnd();
                    }
                    JsonState::InGAString => {
                        pjson_reader.process_state_InGAString();
                    }
                    JsonState::InOneLineComment => {
                        pjson_reader.process_state_InOneLineComment();
                    }
                },
                None => {
                    break;
                }
            }

            pjson_reader.index += 1;
        }
        pjson_reader.out_buffer
    }

    fn process_state_None(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'{' => {
                self.out_buffer.push(ch);
                self.states.push_back(JsonState::PsKey);
            }
            b'[' => {
                self.states.push_back(JsonState::InArray);
                self.out_buffer.push(ch);
            }
            b'/' => {
                if self.index + 1 < self.pjson.len() && self.pjson[self.index + 1] == b'/' {
                    self.states.push_back(JsonState::InOneLineComment);
                } else {
                    self.out_buffer.push(ch);
                }
            }
            _ => {
                self.out_buffer.push(ch);
            }
        }
    }

    fn process_state_InArray(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'"' => {
                self.states.push_back(JsonState::InString);
                self.out_buffer.push(ch);
            }
            b'`' => {
                self.states.push_back(JsonState::InGAStringStart);
                self.GA_start_index = self.index;
                self.out_buffer.push(b'"');
            }
            b'{' => {
                self.out_buffer.push(ch);
                self.states.push_back(JsonState::PsKey);
            }
            b'[' => {
                self.states.push_back(JsonState::InArray);
                self.out_buffer.push(ch);
            }
            b']' => {
                self.states.pop_back();
                self.eat_ex_comma();
                self.out_buffer.push(ch);
            }
            b'/' => {
                if self.index + 1 < self.pjson.len() && self.pjson[self.index + 1] == b'/' {
                    self.states.push_back(JsonState::InOneLineComment);
                } else {
                    self.out_buffer.push(ch);
                }
            }
            _ => {
                self.out_buffer.push(ch);
            }
        }
    }

    fn process_state_PsKey(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'"' => {
                self.states.push_back(JsonState::InString);
                self.out_buffer.push(ch);
            }
            b'`' => {
                self.states.push_back(JsonState::InGAStringStart);
                self.GA_start_index = self.index;
                self.out_buffer.push(b'"');
            }
            b':' => {
                self.states.pop_back();
                self.states.push_back(JsonState::PsValue);
                self.out_buffer.push(ch);
            }
            b'/' => {
                if self.index + 1 < self.pjson.len() && self.pjson[self.index + 1] == b'/' {
                    self.states.push_back(JsonState::InOneLineComment);
                } else {
                    self.out_buffer.push(ch);
                }
            }
            b'}' => {
                self.states.pop_back();
                self.eat_ex_comma();
                self.out_buffer.push(ch);
            }
            b']' => {
                self.states.pop_back();
                self.eat_ex_comma();
                self.out_buffer.push(ch);
            }
            _ => {
                if ch.is_ascii_graphic() {
                    self.process_no_quotation_key();
                } else {
                    self.out_buffer.push(ch);
                }
            }
        }
    }

    // 处理不带双引号的 key
    fn process_no_quotation_key(&mut self) {
        self.out_buffer.push(b'"');
        while self.index < self.pjson.len()
            && self.pjson[self.index] != b':'
            && self.pjson[self.index].is_ascii_graphic()
        {
            self.out_buffer.push(self.pjson[self.index]);
            self.index += 1;
        }
        self.out_buffer.push(b'"');
        if self.index > 0 {
            self.index -= 1;
        }
    }

    fn process_state_PsValue(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'"' => {
                self.states.push_back(JsonState::InString);
                self.out_buffer.push(ch);
            }
            b'`' => {
                self.states.push_back(JsonState::InGAStringStart);
                self.GA_start_index = self.index;
                self.out_buffer.push(b'"');
            }
            b'{' => {
                self.out_buffer.push(ch);
                self.states.push_back(JsonState::PsKey);
            }
            b'[' => {
                self.states.push_back(JsonState::InArray);
                self.out_buffer.push(ch);
            }
            b',' => {
                self.states.pop_back();
                self.states.push_back(JsonState::PsKey);
                self.out_buffer.push(ch);
            }
            b'}' => {
                self.states.pop_back();
                self.eat_ex_comma();
                self.out_buffer.push(ch);
            }
            b']' => {
                // 不会进入这里, 因为 PsValue 是属于 object 的
            }
            b'/' => {
                if self.index + 1 < self.pjson.len() && self.pjson[self.index + 1] == b'/' {
                    self.states.push_back(JsonState::InOneLineComment);
                } else {
                    self.out_buffer.push(ch);
                }
            }
            _ => {
                self.out_buffer.push(ch);
            }
        }
    }

    fn process_state_InEscape(&mut self) -> () {
        let ch = self.pjson[self.index];
        self.states.pop_back();
        self.out_buffer.push(ch);
    }

    fn process_state_InOneLineComment(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'\n' => {
                self.states.pop_back();
                self.out_buffer.push(ch);
            }
            _ => {
                // 注释内容不输出到最终JSON
            }
        }
    }

    fn process_state_InString(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'"' => {
                self.states.pop_back();
                self.out_buffer.push(ch);
            }
            b'\\' => {
                self.states.push_back(JsonState::InEscape);
                self.out_buffer.push(ch);
            }
            _ => {
                // 转义JSON特殊字符
                self.escape_json_char(ch);
            }
        }
    }

    fn process_state_InGAStringStart(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'`' => {
                self.states.pop_back();
                self.states.push_back(JsonState::InGAString);
                self.GA_Tag =
                    String::from_utf8_lossy(&self.pjson[self.GA_start_index + 1..self.index])
                        .to_string();
            }
            _ => {
                // do nothing
            }
        }
    }

    fn process_state_InGAStringEnd(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'`' => {
                self.states.pop_back();

                // 获取GA字符串内容并转义
                let content = String::from_utf8_lossy(&self.GA_temp_string_buffer);
                let escaped_content = self.escape_json_string(&content);

                self.out_buffer
                    .extend_from_slice(escaped_content.as_bytes());
                self.GA_temp_string_buffer.clear();
                self.out_buffer.push(b'"');
            }
            _ => {
                // do nothing
            }
        }
    }

    fn process_state_InGAString(&mut self) -> () {
        let ch = self.pjson[self.index];
        match ch {
            b'`' => {
                // 检查是否是结束标记
                let tag_end_index = self.index + 1 + self.GA_Tag.len();
                if tag_end_index < self.pjson.len()
                    && self.pjson[tag_end_index] == b'`'
                    && String::from_utf8_lossy(&self.pjson[self.index + 1..tag_end_index])
                        == self.GA_Tag
                {
                    self.states.pop_back();
                    self.states.push_back(JsonState::InGAStringEnd);
                    // 跳过标签字符，后续循环会自动增加index
                    self.index += self.GA_Tag.len();
                } else {
                    self.GA_temp_string_buffer.push(ch);
                }
            }
            _ => {
                self.GA_temp_string_buffer.push(ch);
            }
        }
    }

    // 去除最后一个成员的 b','
    fn eat_ex_comma(&mut self) {
        let mut p = self.out_buffer.len().checked_sub(1);
        while let Some(pos) = p {
            if !self.out_buffer[pos].is_ascii_graphic() {
                p = pos.checked_sub(1);
            } else {
                break;
            }
        }

        if let Some(pos) = p {
            if self.out_buffer[pos] == b',' {
                self.out_buffer.remove(pos);
            }
        }
    }

    // 转义单个JSON字符
    fn escape_json_char(&mut self, ch: u8) {
        match ch {
            b'"' => {
                self.out_buffer.extend_from_slice(b"\\\"");
            }
            b'\\' => {
                self.out_buffer.extend_from_slice(b"\\\\");
            }
            b'\x08' => {
                // backspace
                self.out_buffer.extend_from_slice(b"\\b");
            }
            b'\x0C' => {
                // form feed
                self.out_buffer.extend_from_slice(b"\\f");
            }
            b'\n' => {
                self.out_buffer.extend_from_slice(b"\\n");
            }
            b'\r' => {
                self.out_buffer.extend_from_slice(b"\\r");
            }
            b'\t' => {
                self.out_buffer.extend_from_slice(b"\\t");
            }
            _ => {
                // 处理控制字符 (U+0000 到 U+001F)
                if ch <= 0x1F {
                    let hex = format!("\\u{:04x}", ch);
                    self.out_buffer.extend_from_slice(hex.as_bytes());
                } else {
                    self.out_buffer.push(ch);
                }
            }
        }
    }

    // 转义整个JSON字符串
    fn escape_json_string(&self, input: &str) -> String {
        let mut result = String::new();
        for ch in input.chars() {
            match ch {
                '"' => result.push_str("\\\""),
                '\\' => result.push_str("\\\\"),
                '\x08' => result.push_str("\\b"),
                '\x0C' => result.push_str("\\f"),
                '\n' => result.push_str("\\n"),
                '\r' => result.push_str("\\r"),
                '\t' => result.push_str("\\t"),
                _ => {
                    if ch <= '\u{001F}' {
                        // 控制字符转换为Unicode转义序列
                        result.push_str(&format!("\\u{:04x}", ch as u32));
                    } else {
                        result.push(ch);
                    }
                }
            }
        }
        result
    }
}
