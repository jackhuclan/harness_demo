using OpenAI;
using OpenAI.Chat;
using HarnessDemo.Abstractions;
using System.ClientModel;
// 别名：避免我们自己的 ChatMessage 与 OpenAI.Chat.ChatMessage 冲突
using AbstractionsChat = HarnessDemo.Abstractions.ChatMessage;
using OpenAIChat = OpenAI.Chat.ChatMessage;

namespace HarnessDemo.Model;

/// <summary>
/// 模型层：对接 OpenAI 兼容接口的真实大模型客户端。
/// 默认指向本地 vLLM 网关（.env 配置），也可换成 OpenAI / Azure / 通义 / 文心。
/// 负责：上下文协议转换 + function calling（Planner 决策）。
/// </summary>
public sealed class OpenAiLlmClient : ILlmClient
{
    private readonly ChatClient _chat;
    private readonly int _maxTokens;

    public OpenAiLlmClient(string endpoint, string apiKey, string model, int maxTokens = 4096)
    {
        var options = new OpenAIClientOptions { Endpoint = new Uri(endpoint) };
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        _chat = client.GetChatClient(model);
        _maxTokens = maxTokens;
    }

    public async Task<LlmDecision> DecideAsync(
        IReadOnlyList<AbstractionsChat> context,
        IReadOnlyList<ITool> availableTools,
        CancellationToken ct)
    {
        try
        {
            var messages = ToOpenAiMessages(context);
            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = _maxTokens,
                Temperature = 0.6f,
            };
            foreach (var tool in availableTools)
            {
                // 给每个能力层工具注册一个 function tool（参数 schema 用通用 object）
                options.Tools.Add(ChatTool.CreateFunctionTool(
                    tool.Name,
                    tool.Description,
                    BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}")));
            }

            var result = await _chat.CompleteChatAsync(messages, options, ct);
            var resp = result.Value;

            // 模型决定调用工具 -> 返回工具名 + JSON 参数，由 Controller 调度
            if (resp.FinishReason == ChatFinishReason.ToolCalls)
            {
                var call = resp.ToolCalls.First();
                var args = call.FunctionArguments.ToString();
                Console.WriteLine($"[Model/OpenAI] 模型请求工具调用 {call.FunctionName}({args})");
                return new LlmDecision(true, call.FunctionName, args, null);
            }

            // 最终答复（reasoning 模型的思考链可能分段，只取文本段）
            var text = string.Concat(resp.Content
                .Where(p => p.Text is not null)
                .Select(p => p.Text));
            Console.WriteLine($"[Model/OpenAI] 生成最终答复（{text.Length} 字）");
            return new LlmDecision(false, null, null, text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Model/OpenAI] 调用失败：{ex.Message}");
            return new LlmDecision(false, null, null, $"[模型调用失败] {ex.Message}");
        }
    }

    /// <summary>把我们的上下文消息翻译成 OpenAI Chat 消息协议。</summary>
    private static List<OpenAIChat> ToOpenAiMessages(IReadOnlyList<AbstractionsChat> context)
    {
        var list = new List<OpenAIChat>(context.Count);
        for (var i = 0; i < context.Count; i++)
        {
            var m = context[i];
            switch (m.Role)
            {
                case Role.System:
                    list.Add(new SystemChatMessage(m.Content));
                    break;
                case Role.User:
                    list.Add(new UserChatMessage(m.Content));
                    break;
                case Role.Assistant:
                    // 历史里助手的「调用工具 X」按普通文本回传（SDK 不要求重建 tool_calls 结构）
                    list.Add(new AssistantChatMessage(m.Content));
                    break;
                case Role.Tool:
                    // 工具结果消息：tool_call_id 用合成值指向前一条助手消息
                    list.Add(new ToolChatMessage($"call-{i - 1}", m.Content));
                    break;
            }
        }
        return list;
    }
}
