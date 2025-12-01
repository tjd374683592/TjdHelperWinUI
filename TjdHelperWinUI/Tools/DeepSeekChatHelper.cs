using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TjdHelperWinUI.Tools
{
    public class DeepSeekChatHelper
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly List<Message> _messages;

        public DeepSeekChatHelper(string apiKey, string model = "deepseek-chat")
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.deepseek.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _model = model;
            _messages = new List<Message>();
            _messages.Add(new Message { Role = "system", Content = "你是一个有礼貌的助手" });
        }

        public void AddUserMessage(string content)
        {
            _messages.Add(new Message { Role = "user", Content = content });
        }

        public void AddAssistantMessage(string content)
        {
            _messages.Add(new Message { Role = "assistant", Content = content });
        }

        /// <summary>
        /// 重置会话，保留 system 消息
        /// </summary>
        public void ResetSession()
        {
            _messages.Clear();
            _messages.Add(new Message { Role = "system", Content = "你是一个有礼貌的助手" });
        }

        public async Task<string> GetReplyAsync(int maxTokens = 8192, double temperature = 1.0)
        {
            var requestBody = new
            {
                messages = _messages,
                model = _model,
                max_tokens = maxTokens,
                temperature = temperature,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("chat/completions", content);
            }
            catch (Exception ex)
            {
                throw new Exception("请求失败：" + ex.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"API 返回错误 {response.StatusCode}: {errorText}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);

            string reply = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            _messages.Add(new Message { Role = "assistant", Content = reply });

            return reply;
        }

        public async Task StreamReplyAsync(Action<string> onPartialReply, int maxTokens = 4096, double temperature = 1.0)
        {
            var requestBody = new
            {
                messages = _messages,
                model = _model,
                max_tokens = maxTokens,
                temperature = temperature,
                stream = true
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception ex)
            {
                throw new Exception("请求失败：" + ex.Message);
            }

            if (!response.IsSuccessStatusCode)
            {
                string errorText = await response.Content.ReadAsStringAsync();
                throw new Exception($"API 返回错误 {response.StatusCode}: {errorText}");
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            var assistantBuffer = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!line.StartsWith("data: ")) continue;

                var jsonPart = line.Substring(6);
                if (jsonPart == "[DONE]") break;

                using var doc = JsonDocument.Parse(jsonPart);
                var choices = doc.RootElement.GetProperty("choices")[0];

                if (choices.TryGetProperty("delta", out var delta) && delta.TryGetProperty("content", out var contentElement))
                {
                    var deltaContent = contentElement.GetString();
                    if (!string.IsNullOrEmpty(deltaContent))
                    {
                        onPartialReply(deltaContent);
                        assistantBuffer.Append(deltaContent);
                    }
                }
            }

            _messages.Add(new Message { Role = "assistant", Content = assistantBuffer.ToString() });
        }

        private class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }
        }
    }
}