using System;
using System.IO;
using System.Linq;
using PJson;

namespace PJsonToJson
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("PJson to JSON Converter");
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
            Console.WriteLine("1. Enter PJson text directly");
            Console.WriteLine("2. Convert a single PJson file");
            Console.WriteLine("3. Convert all PJson files in a directory");
            Console.WriteLine("4. Convert all PJson files in a directory (recursive)");
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
            Console.Write("\nEnter PJson file path: ");
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

        private static void ConvertSingleFile(string pjsonFilePath)
        {
            Console.WriteLine($"\nConverting single file: {pjsonFilePath}");

            try
            {
                string pjsonContent = File.ReadAllText(pjsonFilePath);
                string jsonContent = PJsonReader.FromPJson(pjsonContent);

                // 生成输出文件名
                string outputPath;
                if (pjsonFilePath.EndsWith(".pjson", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = pjsonFilePath.Substring(0, pjsonFilePath.Length - 6) + ".json";
                }
                else
                {
                    outputPath = pjsonFilePath + ".json";
                }

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

                File.WriteAllText(outputPath, jsonContent);

                Console.WriteLine($"✓ Successfully converted!");
                Console.WriteLine($"  Output: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to convert {Path.GetFileName(pjsonFilePath)}: {ex.Message}");
            }
        }

        private static void ConvertDirectory(string directoryPath, bool recursive)
        {
            Console.WriteLine($"\nConverting directory: {directoryPath}");
            Console.WriteLine($"Recursive: {(recursive ? "Yes" : "No")}");

            SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 查找所有PJson文件（支持多种大小写）
            var pjsonFiles = Directory.GetFiles(directoryPath, "*.pjson", searchOption)
                                     .Concat(Directory.GetFiles(directoryPath, "*.PJSON", searchOption))
                                     .Concat(Directory.GetFiles(directoryPath, "*.PJson", searchOption))
                                     .Distinct()
                                     .ToArray();

            if (pjsonFiles.Length == 0)
            {
                Console.WriteLine("No PJson files found in the directory.");
                return;
            }

            Console.WriteLine($"Found {pjsonFiles.Length} PJson file(s).");
            Console.WriteLine();

            int successCount = 0;
            int skipCount = 0;

            foreach (var pjsonFile in pjsonFiles)
            {
                try
                {
                    string pjsonContent = File.ReadAllText(pjsonFile);
                    string jsonContent = PJsonReader.FromPJson(pjsonContent);

                    // 生成输出文件名
                    string outputPath;
                    if (pjsonFile.EndsWith(".pjson", StringComparison.OrdinalIgnoreCase))
                    {
                        outputPath = pjsonFile.Substring(0, pjsonFile.Length - 6) + ".json";
                    }
                    else
                    {
                        outputPath = pjsonFile + ".json";
                    }

                    // 检查输出文件是否已存在且内容相同
                    if (File.Exists(outputPath))
                    {
                        string existingContent = File.ReadAllText(outputPath);
                        if (existingContent == jsonContent)
                        {
                            Console.WriteLine($"✓ Skipped (already up-to-date): {Path.GetFileName(pjsonFile)}");
                            skipCount++;
                            continue;
                        }
                    }

                    File.WriteAllText(outputPath, jsonContent);
                    Console.WriteLine($"✓ Converted: {Path.GetFileName(pjsonFile)} → {Path.GetFileName(outputPath)}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Failed to convert {Path.GetFileName(pjsonFile)}: {ex.Message}");
                }
            }

            Console.WriteLine($"\nConversion summary:");
            Console.WriteLine($"  Total files: {pjsonFiles.Length}");
            Console.WriteLine($"  Successfully converted: {successCount}");
            Console.WriteLine($"  Skipped (up-to-date): {skipCount}");
            Console.WriteLine($"  Failed: {pjsonFiles.Length - successCount - skipCount}");
        }

        private static void ConvertDirectInputInteractive()
        {
            Console.WriteLine("\nEnter PJson text (press Ctrl+Z then Enter when done):");
            Console.WriteLine("-------------------------------------------------------");

            var input = ReadMultiLineInput();

            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    string json = PJsonReader.FromPJson(input);

                    Console.WriteLine("\nConverted JSON:");
                    Console.WriteLine("---------------");
                    Console.WriteLine(json);

                    // 询问是否保存到文件
                    Console.Write("\nSave to file? (y/n): ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        SaveToFile(json, ".json");
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
            string pjsonInput = string.Join(" ", args);

            try
            {
                string json = PJsonReader.FromPJson(pjsonInput);
                Console.WriteLine(json);
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
            Console.WriteLine("PJson to JSON Converter");
            Console.WriteLine("Usage:");
            Console.WriteLine("  PJsonToJson.exe                                   - Interactive mode");
            Console.WriteLine("  PJsonToJson.exe <pjson_text>                      - Convert directly from command line");
            Console.WriteLine("  PJsonToJson.exe -f <file_path>                    - Convert single file");
            Console.WriteLine("  PJsonToJson.exe --file <file_path>                - Convert single file");
            Console.WriteLine("  PJsonToJson.exe -d <directory_path>               - Convert all PJson files in directory");
            Console.WriteLine("  PJsonToJson.exe --dir <directory_path>            - Convert all PJson files in directory");
            Console.WriteLine("  PJsonToJson.exe -d -r <directory_path>            - Convert all PJson files recursively");
            Console.WriteLine("  PJsonToJson.exe --dir --recursive <directory_path>- Convert all PJson files recursively");
            Console.WriteLine("  PJsonToJson.exe -h                                - Show this help");
            Console.WriteLine("\nExamples:");
            Console.WriteLine("  PJsonToJson.exe '{name:`John`}'");
            Console.WriteLine("  PJsonToJson.exe -f data.pjson");
            Console.WriteLine("  PJsonToJson.exe -d ./config/");
            Console.WriteLine("  PJsonToJson.exe --dir C:\\Users\\Name\\Documents\\");
            Console.WriteLine("  PJsonToJson.exe -d -r ./data/");
            Console.WriteLine("\nNote: Directory mode only converts *.pjson files, does not recurse into subdirectories by default.");
        }
    }
}