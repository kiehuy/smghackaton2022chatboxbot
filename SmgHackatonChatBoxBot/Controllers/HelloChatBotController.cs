/*******************************************************************************
* Copyright 2009-2020 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using dotnetLexChatBot.Models;
using dotnetLexChatBot.Data;
using Amazon.Lex;
//using System.Web.Mvc;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace dotnetLexChatBot.Controllers
{
    public class HelloChatBotController : Controller
    {
        //Collection of ChatBot Messages

        //static Dictionary<string, string> lexSessionData = new Dictionary<string, string>();
        private readonly IAWSLexService awsLexSvc;
        private ISession userHttpSession;
        private Dictionary<string, string> lexSessionData;
        private List<ChatBotMessage> botMessages;
        private string botMsgKey = "ChatBotMessages", 
                       botAtrribsKey = "LexSessionData",
                       userSessionID = String.Empty;


        public HelloChatBotController(IAWSLexService awsLexService)
        {
            awsLexSvc = awsLexService;
        }

        public async Task<IActionResult> TestChat(List<ChatBotMessage> messages)
        {
            userHttpSession = HttpContext.Session;
            userSessionID = userHttpSession.Id;
            botMessages = userHttpSession.Get<List<ChatBotMessage>>(botMsgKey) ?? new List<ChatBotMessage>();
            lexSessionData = userHttpSession.Get<Dictionary<string, string>>(botAtrribsKey) ?? new Dictionary<string, string>();

            //A Valid Message exists, Add to page and allow Lex to process
            botMessages.Add(new ChatBotMessage()
            { MsgType = MessageType.UserMessage, ChatMessage = "Hello" });

            //await postUserData(botMessages);

            //Call Amazon Lex with Text, capture response
            var lexResponse = await awsLexSvc.SendTextMsgToLex("Hello", lexSessionData, userSessionID);

            return View(messages);
        }

        [HttpPost]
        public async Task<JsonResult> GetChatMessage(string userMessage)
        {
            userHttpSession = HttpContext.Session;
            userSessionID = userHttpSession.Id;
            botMessages = userHttpSession.Get<List<ChatBotMessage>>(botMsgKey) ?? new List<ChatBotMessage>();
            lexSessionData = userHttpSession.Get<Dictionary<string, string>>(botAtrribsKey) ?? new Dictionary<string, string>();

            //A Valid Message exists, Add to page and allow Lex to process
            botMessages.Add(new ChatBotMessage()
            { MsgType = MessageType.UserMessage, ChatMessage = userMessage });

            //await postUserData(botMessages);

            //Call Amazon Lex with Text, capture response
            var lexResponse = await awsLexSvc.SendTextMsgToLex(userMessage, lexSessionData, userSessionID);

            if (lexResponse != null && !string.IsNullOrEmpty(lexResponse.Message))
            {
                if (lexResponse.Message.Contains("[report]"))
                {
                    lexResponse.Message = lexResponse.Message.Replace("[report]", "<a id='openReport' href='/HelloChatBot/GetReport' target='_blank'>Open your report</a>");
                }
            }

            lexSessionData = lexResponse.SessionAttributes;
            if (
                lexResponse.DialogState == DialogState.ElicitSlot ||
                lexResponse.DialogState == DialogState.ConfirmIntent
            )
            {
                botMessages.Add(
                    new ChatBotMessage()
                    {
                        MsgType = MessageType.LexMessage,
                        ChatMessage = lexResponse.Message
                    });
            }
            else if (
              lexResponse.DialogState == DialogState.ReadyForFulfillment ||
              lexResponse.DialogState == DialogState.Fulfilled
          )
            {
                botMessages.Add(
                    new ChatBotMessage()
                    {
                        MsgType = MessageType.LexMessage,
                        ChatMessage = lexResponse.Message ?? "Your order is being processed. Thank you for your business!"
                    });
            }

            //Add updated botMessages and lexSessionData object to Session
            userHttpSession.Set<List<ChatBotMessage>>(botMsgKey, botMessages);
            userHttpSession.Set<Dictionary<string, string>>(botAtrribsKey, lexSessionData);

            return Json(botMessages);
        }

        public async Task<IActionResult> postUserData(List<ChatBotMessage> messages)
        {
            //testing
            return await Task.Run(() => TestChat(messages));
        }

        public FileResult GetReport()
        {
            byte[] FileBytes = System.IO.File.ReadAllBytes("Views/HelloChatBot/report.pdf");
            return File(FileBytes, "application/pdf");
        }
    }

}
