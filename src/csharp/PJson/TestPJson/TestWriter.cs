using System;
using System.Text.Json;
using PJson;

namespace TestPJson
{
    public class TestWriter
    {
        public static void Test()
        {
            TestSimpleObject();
            TestComplexObject();
            TestStringWithTags();
        }

        public static void TestSimpleObject()
        {
            string json = """
            {
                "name": "John Doe",
                "age": 30,
                "isStudent": false,
                "courses": ["Math", "Science", "History"],
                "address": {
                    "street": "123 Main St",
                    "city": "New York"
                }
            }
            """;

            string pjson = PJsonWriter.ToPJson(json);

            Console.WriteLine("TestSimpleObject:");
            Console.WriteLine("Input JSON:");
            Console.WriteLine(json);
            Console.WriteLine("\nOutput PJson:");
            Console.WriteLine(pjson);
            Console.WriteLine();
        }

        public static void TestComplexObject()
        {
            string json = """
            [
                {
                    "id": 1,
                    "name": "Item 1",
                    "description": "First item with `special` characters",
                    "tags": ["tag1", "tag2", "tag3"],
                    "metadata": {
                        "created": "2024-01-01",
                        "modified": "2024-01-02"
                    }
                },
                {
                    "id": 2,
                    "name": "Item 2",
                    "description": "Second item with ``double backticks``",
                    "active": true,
                    "score": 95.5
                }
            ]
            """;

            string pjson = PJsonWriter.ToPJson(json);

            Console.WriteLine("TestComplexObject:");
            Console.WriteLine("Input JSON:");
            Console.WriteLine(json);
            Console.WriteLine("\nOutput PJson:");
            Console.WriteLine(pjson);
            Console.WriteLine();
        }

        public static void TestStringWithTags()
        {
            string json = """
            {
                "normal": "This is a normal string",
                "withBackticks": "String with `backtick` inside",
                "withDoubleBackticks": "String with ``double`` backticks",
                "withUnderscoreTag": "String that contains `_` tag",
                "multiUnderscoreTag": "String that contains `__` and `___` tags",
                "multiline": "Line 1\nLine 2\nLine 3"
            }
            """;

            string pjson = PJsonWriter.ToPJson(json);

            Console.WriteLine("TestStringWithTags:");
            Console.WriteLine("Input JSON:");
            Console.WriteLine(json);
            Console.WriteLine("\nOutput PJson:");
            Console.WriteLine(pjson);
            Console.WriteLine();
        }

        public static void TestRoundTrip()
        {
            string json = """
            {
                "name": "Test Object",
                "values": [1, 2, 3],
                "nested": {
                    "key1": "value1",
                    "key2": "value2 with `backtick`"
                }
            }
            """;

            Console.WriteLine("TestRoundTrip:");
            Console.WriteLine("Original JSON:");
            Console.WriteLine(json);

            string pjson = PJsonWriter.ToPJson(json);
            Console.WriteLine("\nGenerated PJson:");
            Console.WriteLine(pjson);

            string roundTripJson = PJsonReader.FromPJson(pjson);
            Console.WriteLine("\nRound-trip JSON:");
            Console.WriteLine(roundTripJson);

            // 验证
            try
            {
                var originalObj = JsonDocument.Parse(json);
                var roundTripObj = JsonDocument.Parse(roundTripJson);

                if (originalObj.RootElement.GetRawText() == roundTripObj.RootElement.GetRawText())
                {
                    Console.WriteLine("\n✓ Round-trip conversion successful!");
                }
                else
                {
                    Console.WriteLine("\n✗ Round-trip conversion failed!");
                    Console.WriteLine("\nDifference:");
                    Console.WriteLine($"Original: {originalObj.RootElement.GetRawText()}");
                    Console.WriteLine($"RoundTrip: {roundTripObj.RootElement.GetRawText()}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error in round-trip: {ex.Message}");
            }
        }
    }
}