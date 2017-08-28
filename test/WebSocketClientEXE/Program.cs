using AspNetCoreModule.Test.Framework;
using AspNetCoreModule.Test.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketClientEXE
{
    class Program
    {
        static void Main(string[] args)
        {
            using (WebSocketClientHelper websocketClient = new WebSocketClientHelper())
            {
                var frameReturned = websocketClient.Connect(new Uri("http://localhost:40000/aspnetcoreapp/websocket"), true, true);
                //var frameReturned = websocketClient.Connect(new Uri("http://localhost:40000/aspnetcoreapp"), true, true);
                //Assert.Contains("Connection: Upgrade", frameReturned.Content);
                //Assert.Contains("HTTP/1.1 101 Switching Protocols", frameReturned.Content);
                Thread.Sleep(500);

                websocketClient.SendTextData("Test");
                bool connectionClosedFromServer = false;
                for (int i = 0; i < 10000; i++)
                {
                    if (websocketClient.Connection.Done)
                    {
                        connectionClosedFromServer = true;
                        break;
                    }
                    else
                    {
                        Thread.Sleep(500);
                    }
                }

                if (connectionClosedFromServer)
                {
                    TestUtility.LogInformation("Success");
                }
                //Assert.True(connectionClosedFromServer, "Closing Handshake initiated from Server");

                //frameReturned = websocketClient.ReadData();
                //Assert.True(frameReturned.FrameType == FrameType.Close, "Closing Handshake");
                //string dataReturned = System.Text.Encoding.ASCII.GetString(frameReturned.Data);
                //Assert.Equal("ClosingFromServer", dataReturned);
                websocketClient.Connection.Done = true;
            }
        }
    }
}
