#define UseOptions // or NoOptions
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EchoApp
{
    public class Startup
    {
        const int maxConnection = 100;
        private WebSocket[] _websockets;
        private int[] _index;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public int getAvailableIndex()
        {
            for (int i = 0; i < maxConnection; i++)
            {
                if (_index[i] == -1)
                {
                    _index[i] = 0;
                    return i;
                }
            }
            return -1;
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _websockets = new WebSocket[maxConnection];
            _index = new int[maxConnection];
            for (int i = 0; i < maxConnection; i++)
            {
                _websockets[i] = null;
                _index[i] = -1;
            }

            loggerFactory.AddConsole(LogLevel.Debug);
            loggerFactory.AddDebug(LogLevel.Debug);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

#if NoOptions
            #region UseWebSockets
            app.UseWebSockets();
            #endregion
#endif
#if UseOptions
            #region UseWebSocketsOptions
            var webSocketOptions = new WebSocketOptions()
            {
                KeepAliveInterval = TimeSpan.FromSeconds(120),
                ReceiveBufferSize = 4 * 1024
            };
            app.UseWebSockets(webSocketOptions);
            #endregion
#endif
            #region AcceptWebSocket
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        int index = this.getAvailableIndex();
                        if (index == -1)
                        {
                            throw new System.Exception("No available connection");
                        }
                        _websockets[index] = await context.WebSockets.AcceptWebSocketAsync();
                        await Echo(context, index);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await next();
                }

            });
            #endregion
            app.UseFileServer();
        }
        #region Echo

        private async void broadCast(byte[] buffer, WebSocketReceiveResult result)
        {
            for (int i = 0; i < maxConnection; i++)
            {
                if (_index[i] != -1)
                {
                    await _websockets[i].SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, CancellationToken.None);
                }
            }
        }

        private bool IsValidCommand(string command)
        {
            if (command.StartsWith("CloseFromServerBugRepro") || command.StartsWith("CloseFromServerWithIgnoringExceptionError"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private async Task CommandHandler(string command, int index)
        {
            if (command.StartsWith("CloseFromServerBugRepro"))
            {
                await _websockets[index].CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "Closing from server...", CancellationToken.None);
            }
        }

        private async Task Echo(HttpContext context, int index)
        {
            var buffer = new byte[1024 * 4];
            string tempMessage = "Connection " + index + " opened...";
            WebSocketReceiveResult tempResult = new WebSocketReceiveResult(tempMessage.Length, WebSocketMessageType.Text, true);
            broadCast(Encoding.ASCII.GetBytes(tempMessage), tempResult);
            
            WebSocketReceiveResult result = await _websockets[index].ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                if (IsValidCommand(System.Text.Encoding.ASCII.GetString(buffer)))
                {
                    await CommandHandler(System.Text.Encoding.ASCII.GetString(buffer), index);
                }
                else
                {
                    broadCast(buffer, result);
                }
                result = await _websockets[index].ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            if (_index[index] != 0)
            {
                throw new System.Exception("index value is not 0");
            }
            else
            {
                _index[index] = -1;
            }

            await _websockets[index].CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            _websockets[index] = null;

            string tempMessage2 = "Connection " + index + " closed...";
            WebSocketReceiveResult tempResult2 = new WebSocketReceiveResult(tempMessage2.Length, WebSocketMessageType.Text, true);
            broadCast(Encoding.ASCII.GetBytes(tempMessage2), tempResult2);
        }
        #endregion
    }
}