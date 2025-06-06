﻿using Azure;
using Backend.Core;
using Backend.Core.Exceptions;
using OpenAI.Chat;

namespace Backend.Infrastructure.AzureOpenAI
{
    public class OpenAIService(ChatClient chatClient, ITokenizerService tokenizerService) : IOpenAIService
    {
        private readonly ChatClient _chatClient = chatClient;
        private readonly ITokenizerService _tokenizerService = tokenizerService;
        private const int MaxTokens = 8000; // Example token limit for GPT-4
        private const int PortionSize = 30; // Number of messages per portion

        public async Task<string> GetChatResponseAsync(IEnumerable<Core.Models.ChatMessage> messages)
        {
            var chatMessages = PrepareChatMessages(messages);            

            try
            {
                return await GetChatResponseAsync(chatMessages);
            }
            catch(Exception ex)
            {
                if (ex.Message.Contains("HTTP 429"))
                {
                    throw new OpenAIRateLimitException();
                }
                else throw;
            }
        }

        public async IAsyncEnumerable<string> GetChatResponseStreamingAsync(IEnumerable<Core.Models.ChatMessage> messages)
        {
            var chatMessages = PrepareChatMessages(messages);
            await using var enumerator = _chatClient.CompleteChatStreamingAsync(chatMessages).GetAsyncEnumerator();
            bool yieldedAny = false;

            while (true)
            {
                bool hasResult;
                try
                {
                    hasResult = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("HTTP 429"))
                        throw new OpenAIRateLimitException();
                    throw;
                }

                if (!hasResult)
                    break;

                var completionUpdate = enumerator.Current;

                if (completionUpdate == null || completionUpdate.ContentUpdate == null)
                    continue;

                if (completionUpdate.ContentUpdate.Count == 0)
                    continue;

                foreach (var contentPart in completionUpdate.ContentUpdate)
                {
                    yieldedAny = true;
                    yield return contentPart.Text;
                }
            }

            if (!yieldedAny)
                throw new NotFoundException("Chat response not found.");
        }


        private List<ChatMessage> PrepareChatMessages(IEnumerable<Core.Models.ChatMessage> messages)
        {
            var chatMessages = new List<ChatMessage>();
            int totalTokens = 0;

            foreach (var message in messages)
            {
                var tokenCount = _tokenizerService.CountTokens(message.Content);
                if (totalTokens + tokenCount > MaxTokens)
                {
                    // Summarize older messages to fit within the token limit
                    var summarizedMessages = SummarizeMessagesInPortions(messages).Result;
                    chatMessages.AddRange(summarizedMessages);
                    break;
                }

                AddChatMessage(chatMessages, message);
                totalTokens += tokenCount;
            }

            return chatMessages;
        }

        private static void AddChatMessage(List<ChatMessage> chatMessages, Core.Models.ChatMessage message)
        {
            if (message.Role == Backend.Core.Models.ChatRole.System)
            {
                chatMessages.Add(new SystemChatMessage(message.Content));
            }
            else if (message.Role == Backend.Core.Models.ChatRole.User)
            {
                chatMessages.Add(new UserChatMessage(message.Content));
            }
            else if (message.Role == Backend.Core.Models.ChatRole.Assistant)
            {
                chatMessages.Add(new AssistantChatMessage(message.Content));
            }
        }

        private async Task<string> GetChatResponseAsync(List<ChatMessage> chatMessages)
        {
            try
            {
                var response = await _chatClient.CompleteChatAsync(chatMessages);
                if (response == null || response.Value == null)
                {
                    throw new NotFoundException("Chat response not found.");
                }

                return response.Value.Content.Count > 0 ? response.Value.Content[0].Text : string.Empty;
            }
            catch (RequestFailedException ex) when (ex.Status == 400)
            {
                throw new BadRequestException("Invalid request to OpenAI API.");
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                throw new NotFoundException("OpenAI API endpoint not found.");
            }
            catch (NotFoundException)
            {
                // Rethrow NotFoundException to ensure it is not caught by the generic Exception block
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred: {ex.Message}");
            }
        }

        private async Task<IEnumerable<ChatMessage>> SummarizeMessagesInPortions(IEnumerable<Core.Models.ChatMessage> messages)
        {
            var summarizedMessages = new List<ChatMessage>();
            var messageList = messages.ToList();

            for (int i = 0; i < messageList.Count; i += PortionSize)
            {
                var portion = messageList.Skip(i).Take(PortionSize);
                var summary = await Summarize(portion);
                summarizedMessages.Add(new SystemChatMessage(summary));
            }

            return summarizedMessages;
        }

        private async Task<string> Summarize(IEnumerable<Core.Models.ChatMessage> messages)
        {
            var chatMessages = new List<ChatMessage>
            {
                new UserChatMessage("Summarize the following messages:")
            };

            foreach (var message in messages)
            {
                AddChatMessage(chatMessages, message);
            }

            var response = await _chatClient.CompleteChatAsync(chatMessages);
            return response.Value.Content.Count > 0 ? response.Value.Content[0].Text : string.Empty;
        }
    }
}