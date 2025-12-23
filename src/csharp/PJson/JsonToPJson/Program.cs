using System;
using System.IO;
using System.Linq;
using PJson;

namespace JsonToPJson
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("JSON to PJson Converter");
            Console.WriteLine("========================");

            try
            {
                if (args.Length == 0)
                {
                    // 交互模式
                    RunInteractiveMode();
                }
                else if (args.Length == 2 && (args[0] == "-f" || args[0] == "--file"))
                {
                    // 单个文件模式
                    ConvertSingleFile(args[1]);
                }
                else if (args.Length == 2 && (args[0] == "-d" || args[0] == "--dir"))
                {
                    // 目录模式
                    ConvertDirectory(args[1], false);
                }
                else if (args.Length == 3 && (args[0] == "-d" || args[0] == "--dir") && args[1] == "-r")
                {
                    // 目录模式（递归）
                    ConvertDirectory(args[2], true);
                }
                else if (args.Length == 1 && (args[0] == "-h" || args[0] == "--help"))
                {
                    ShowHelp();
                }
                else
                {
                    // 直接输入模式
                    ConvertDirectInput(args);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void RunInteractiveMode()
        {
            Console.WriteLine("\nChoose mode:");
            Console.WriteLine("1. Enter JSON text directly");
            Console.WriteLine("2. Convert a single JSON file");
            Console.WriteLine("3. Convert all JSON files in a directory");
            Console.WriteLine("4. Convert all JSON files in a directory (recursive)");
            Console.Write("Select option (1-4): ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ConvertDirectInputInteractive();
                    break;
                case "2":
                    ConvertSingleFileInteractive();
                    break;
                case "3":
                    ConvertDirectoryInteractive(false);
                    break;
                case "4":
                    ConvertDirectoryInteractive(true);
                    break;
                default:
                    Console.WriteLine("Invalid choice!");
                    break;
            }
        }

        private static void ConvertSingleFileInteractive()
        {
            Console.Write("\nEnter JSON file path: ");
            string filePath = Console.ReadLine()?.Trim('"');

            if (File.Exists(filePath))
            {
                ConvertSingleFile(filePath);
            }
            else
            {
                Console.WriteLine("File not found!");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ConvertDirectoryInteractive(bool recursive)
        {
            Console.Write("\nEnter directory path: ");
            string dirPath = Console.ReadLine()?.Trim('"');

            if (Directory.Exists(dirPath))
            {
                ConvertDirectory(dirPath, recursive);
            }
            else
            {
                Console.WriteLine("Directory not found!");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ConvertSingleFile(string jsonFilePath)
        {
            Console.WriteLine($"\nConverting single file: {jsonFilePath}");

            try
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                string pjsonContent = PJsonWriter.ToPJson(jsonContent);

                // 生成输出文件名（替换扩展名）
                string outputPath = Path.ChangeExtension(jsonFilePath, ".pjson");

                // 如果目标文件已存在，询问是否覆盖
                if (File.Exists(outputPath))
                {
                    Console.Write($"File '{Path.GetFileName(outputPath)}' already exists. Overwrite? (y/n): ");
                    if (Console.ReadLine()?.ToLower() != "y")
                    {
                        Console.WriteLine("Conversion cancelled.");
                        return;
                    }
                }

                File.WriteAllText(outputPath, pjsonContent);

                Console.WriteLine($"✓ Successfully converted!");
                Console.WriteLine($"  Output: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to convert {Path.GetFileName(jsonFilePath)}: {ex.Message}");
            }
        }

        private static void ConvertDirectory(string directoryPath, bool recursive)
        {
            Console.WriteLine($"\nConverting directory: {directoryPath}");
            Console.WriteLine($"Recursive: {(recursive ? "Yes" : "No")}");

            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 查找所有JSON文件
            var jsonFiles = Directory.GetFiles(directoryPath, "*.json", searchOption)
                                    .Concat(Directory.GetFiles(directoryPath, "*.JSON", searchOption))
                                    .Distinct()
                                    .ToArray();

            if (jsonFiles.Length == 0)
            {
                Console.WriteLine("No JSON files found in the directory.");
                return;
            }

            Console.WriteLine($"Found {jsonFiles.Length} JSON file(s).");
            Console.WriteLine();

            int successCount = 0;
            int skipCount = 0;

            foreach (var jsonFile in jsonFiles)
            {
                try
                {
                    string jsonContent = File.ReadAllText(jsonFile);
                    string pjsonContent = PJsonWriter.ToPJson(jsonContent);

                    // 生成输出文件名
                    string outputPath = Path.ChangeExtension(jsonFile, ".pjson");

                    // 检查输出文件是否已存在且内容相同
                    if (File.Exists(outputPath))
                    {
                        string existingContent = File.ReadAllText(outputPath);
                        if (existingContent == pjsonContent)
                        {
                            Console.WriteLine($"✓ Skipped (already up-to-date): {Path.GetFileName(jsonFile)}");
                            skipCount++;
                            continue;
                        }
                    }

                    File.WriteAllText(outputPath, pjsonContent);
                    Console.WriteLine($"✓ Converted: {Path.GetFileName(jsonFile)} → {Path.GetFileName(outputPath)}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to convert {Path.GetFileName(jsonFile)}: {ex.Message}");
                }
            }

            Console.WriteLine($"\nConversion summary:");
            Console.WriteLine($"  Total files: {jsonFiles.Length}");
            Console.WriteLine($"  Successfully converted: {successCount}");
            Console.WriteLine($"  Skipped (up-to-date): {skipCount}");
            Console.WriteLine($"  Failed: {jsonFiles.Length - successCount - skipCount}");
        }

        private static void ConvertDirectInputInteractive()
        {
            Console.WriteLine("\nEnter JSON text (press Ctrl+Z then Enter when done):");
            Console.WriteLine("-------------------------------------------------------");

            var input = ReadMultiLineInput();

            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    string pjson = PJsonWriter.ToPJson(input);

                    Console.WriteLine("\nConverted PJson:");
                    Console.WriteLine("----------------");
                    Console.WriteLine(pjson);

                    // 询问是否保存到文件
                    Console.Write("\nSave to file? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        SaveToFile(pjson, ".pjson");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Conversion failed: {ex.Message}");
                }
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        private static void ConvertDirectInput(string[] args)
        {
            // 将参数组合成单个字符串
            string jsonInput = string.Join(" ", args);

            try
            {
                string pjson = PJsonWriter.ToPJson(jsonInput);
                Console.WriteLine(pjson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void SaveToFile(string content, string extension)
        {
            string defaultName = $"output{extension}";
            Console.Write($"Enter output file name [{defaultName}]: ");
            string fileName = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = defaultName;
            }

            // 确保有正确的扩展名
            if (!Path.HasExtension(fileName))
            {
                fileName += extension;
            }

            try
            {
                File.WriteAllText(fileName, content);
                Console.WriteLine($"File saved: {Path.GetFullPath(fileName)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file: {ex.Message}");
            }
        }

        private static string ReadMultiLineInput()
        {
            var input = new System.Text.StringBuilder();
            string line;

            while ((line = Console.ReadLine()) != null)
            {
                input.AppendLine(line);
            }

            return input.ToString();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("JSON to PJson Converter");
            Console.WriteLine("Usage:");
            Console.WriteLine("  JsonToPJson.exe                                   - Interactive mode");
            Console.WriteLine("  JsonToPJson.exe <json_text>                       - Convert directly from command line");
            Console.WriteLine("  JsonToPJson.exe -f <file_path>                    - Convert single file");
            Console.WriteLine("  JsonToPJson.exe --file <file_path>                - Convert single file");
            Console.WriteLine("  JsonToPJson.exe -d <directory_path>               - Convert all JSON files in directory");
            Console.WriteLine("  JsonToPJson.exe --dir <directory_path>            - Convert all JSON files in directory");
            Console.WriteLine("  JsonToPJson.exe -d -r <directory_path>            - Convert all JSON files recursively");
            Console.WriteLine("  JsonToPJson.exe --dir --recursive <directory_path>- Convert all JSON files recursively");
            Console.WriteLine("  JsonToPJson.exe -h                                - Show this help");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  JsonToPJson.exe '{\"name\":\"John\"}'");
            Console.WriteLine("  JsonToPJson.exe -f data.json");
            Console.WriteLine("  JsonToPJson.exe -d ./config/");
            Console.WriteLine("  JsonToPJson.exe --dir C:\\Users\\Name\\Documents\\");
            Console.WriteLine("  JsonToPJson.exe -d -r ./data/");
            Console.WriteLine("\nNote: Directory mode only converts *.json files, does not recurse into subdirectories by default.");
        }
    }
}