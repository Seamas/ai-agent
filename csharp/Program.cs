

using System.Linq.Expressions;
using System.Reflection;
using csharp;
using dotenv.net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

class Program
{
    private static Kernel _kernel;
    private static ChatHistory _chatHistory;
    private static IChatCompletionService _chatService;

    static async Task Main(string[] args)
    {
        DotEnv.Load();
        
        var apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("请配置 api key");
            return;
        }

        var builder = Kernel.CreateBuilder();
        
        builder.AddOpenAIChatCompletion(
            modelId: "deepseek-chat",
            endpoint: new Uri("https://api.deepseek.com"),
            apiKey: apiKey
            );

        builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

        _kernel = builder.Build();
        // _kernel.ImportPluginFromFunctions("FileTools", GetFileTools());
        _kernel.ImportPluginFromType<FileTools>("FileTools");
        
        _chatHistory = new ChatHistory();
        _chatHistory.AddSystemMessage("你是一个资深的程序员。你可以使用各种工具来帮助用户解决问题。在每一步中，你需要思考是否需要调用工具来完成任务。如果需要，请调用合适的工具。");

        await ExecuteAgentAsync();
    }


    static async Task ExecuteAgentAsync()
    {
        _chatService = _kernel.GetRequiredService<IChatCompletionService>();

        while (true)
        {
            Console.Write("Input:");

            var userInput = Console.ReadLine();

            if (string.IsNullOrEmpty(userInput))
            {
                continue;
            }

            if (string.Equals(userInput, "exit", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }
            
            _chatHistory.AddUserMessage(userInput);

            await RunAgentIterationAsync();
        }
    }


    static List<KernelFunction> GetFileTools()
    {
        List<KernelFunction> tools = new();

        // 方法1: 注册工具方法
        // var listFileFunction = KernelFunctionFactory.CreateFromMethod(
        //     method: () =>
        //     {
        //         var files = Directory.GetFiles(Directory.GetCurrentDirectory());
        //         return string.Join("\n", files);
        //     },
        //     functionName:"list_files",
        //     description: "列出当前目录下的所有文件");
        //
        // tools.Add(listFileFunction);
        
        // 方法2: 手动注册方法
        // tools.Add(KernelFunctionFactory.CreateFromMethod(
        //     method: (Func<string>)FileTools.ListFiles,
        //     functionName: "list_files",
        //     description: "列出当前目录下的所有文件"
        // ));
        
        // 方法3: 反射自动注册方法
        var methods = typeof(FileTools).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName);  // 过滤掉属性访问器等特殊方法

        foreach (var method in methods)
        {
            var delegateType = GetDelegateType(method);
            var @delegate = method.CreateDelegate(delegateType);

            var func = KernelFunctionFactory.CreateFromMethod(
                method: @delegate,
                functionName: method.Name.ToLowerInvariant(),
                description: GetMethodDescription(method));
            
            tools.Add(func);
        }

        return tools;
    }

    
    /// <summary>
    /// 获取方法的委托类型
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    static Type GetDelegateType(MethodInfo method)
    {
        var parameterInfos = method.GetParameters();
        var paramTypes = parameterInfos.Select(p => p.ParameterType).ToArray();

        if (method.ReturnType == typeof(void))
        {
            return Expression.GetActionType(paramTypes);
        }
        else
        {
            return Expression.GetFuncType(paramTypes.Concat([method.ReturnType]).ToArray());
        }
    }

    static string GetMethodDescription(MethodInfo method)
    {
        var descriptionAttribute = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();
        if (descriptionAttribute != null)
        {
            return descriptionAttribute.Description;
        }
        return method.Name;
    }

    static async Task RunAgentIterationAsync()
    {
        var maxIterations = 10; // 防止无限循环
        var iteration = 0;

        while (iteration < maxIterations)
        {
            iteration++;
            
            // 创建执行设置，启用自动函数调用
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.7,
                MaxTokens = 20000
            };
            
            try
            {
                // 获取 AI 响应
                var response = await _chatService.GetChatMessageContentAsync(
                    _chatHistory,
                    executionSettings,
                    kernel: _kernel
                );

                // 添加助手响应到历史
                _chatHistory.AddAssistantMessage(response.Content);

                // 检查是否有函数调用
                var functionCalls = response.Items.OfType<FunctionCallContent>().ToList();
                
                if (functionCalls.Any())
                {
                    Console.WriteLine($"\n🤖 Agent 正在调用工具...");
                    
                    // 处理函数调用的结果会通过 AutoInvokeKernelFunctions 自动处理
                    // 这里只需要继续循环，让 AI 基于函数调用的结果做出下一步决策
                    
                    // 打印函数调用信息
                    foreach (var functionCall in functionCalls)
                    {
                        Console.WriteLine($"  调用: {functionCall.FunctionName}, 参数: {functionCall.Arguments}");
                    }
                    
                    // 继续下一次迭代，让 AI 处理函数调用的结果
                    continue;
                }

                // 如果没有函数调用，说明 Agent 完成了任务
                Console.WriteLine($"\n🤖 Agent: {response.Content}");
                break; // 退出循环，等待用户下一个输入
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 错误: {ex.Message}");
                break;
            }
        }

        if (iteration >= maxIterations)
        {
            Console.WriteLine("\n⚠️ 达到最大迭代次数，停止执行");
        }
    }
}