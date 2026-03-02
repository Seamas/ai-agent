using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace csharp;

public class FileTools
{
    private static string baseDir = "";

    [KernelFunction("list_files")]
    [Description("列出当前目录下的所有文件")]
    public static string ListFiles()
    {
        var files = Directory.GetFiles(baseDir);
        return string.Join(Environment.NewLine, files);
    }


    [KernelFunction("read_file")]
    [Description("读取指定文件的内容")]
    public static string ReadFile(
        [Description("要读取的文件路径")]string filePath)
    {
        var path = Path.Combine(baseDir, filePath);
        if (File.Exists(path))
        {
            return File.ReadAllText(path);
        }

        return "文件不存在";
    }

    [KernelFunction("rename_file")]
    [Description("重命名文件")]
    public static string RenameFile(
        [Description("原文件路径")]string oldPath, 
        [Description("新文件路径")]string newPath)
    {
        var source = Path.Combine(baseDir, oldPath);
        var dest = Path.Combine(baseDir, newPath);

        if (File.Exists(source))
        {
            File.Move(source, dest);
            return $"文件已重命名为 {newPath}";
        }
        return "文件不存在";
    }
}