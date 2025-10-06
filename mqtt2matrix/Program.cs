// See https://aka.ms/new-console-template for more information

using Matrix.Sdk;
using mqtt2matrix;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using Newtonsoft.Json;

string mqttBaseTopic = Environment.GetEnvironmentVariable("MQTTBASETOPIC") ?? "bot/matrix/actions/";

var mqttTopicRoomTopic = mqttBaseTopic + "roomtopic";
var mqttTopicRoomName = mqttBaseTopic + "roomname";
var mqttTopicRoomAvatar = mqttBaseTopic + "roomavatar";
var mqttTopicRoomMessage = mqttBaseTopic + "roommessage";

var mqttServer = Environment.GetEnvironmentVariable("MQTTSERVER") ?? "localhost";
int? mqttPort = int.Parse(Environment.GetEnvironmentVariable("MQTTPORT") ?? "1883"); 
var mqttUser = Environment.GetEnvironmentVariable("MQTTUSER") ?? "";
var mqttPassword = Environment.GetEnvironmentVariable("MQTTPASSWORD") ?? "";

var matrixServer = Environment.GetEnvironmentVariable("MATRIXSERVER");
var matrixusername = Environment.GetEnvironmentVariable("MATRIXUSERNAME");
var matrixpassword = Environment.GetEnvironmentVariable("MATRIXPASSWORD");
var matrixdeviceId = Environment.GetEnvironmentVariable("MATRIXDEVICEID") ?? "mqtt2matrixbot";
var matrixHostname = matrixServer.Substring("https://".Length);


var closeUrl = "mxc://matrix.c3re.de/YjVhJIHNrbEXmZmwAgplvNCs";
var openUrl = "mxc://matrix.c3re.de/IJHumgqYFRjmRLqRfBnYgLaJ";
var roomid2 = "!hhLRgoyFSLKlgPhgHF:matrix.c3re.de"; //Jarvis Test Room

static Boolean checkHostName(string roomname, string hostname)
{
    var check = true; //roomname.Contains(hostname);
    if (check)
    {
        Console.WriteLine("Roomname: " + roomname + " contains hostname: " + hostname);
    }
    else
    {
        Console.WriteLine("Roomname: " + roomname + " does not contain hostname: " + hostname);
    }
    return check;
}


var mqttFactory = new MqttFactory();

var mqttClient =  mqttFactory.CreateMqttClient();
MqttClientOptions options;

    if ((mqttUser != null && mqttPassword != null) && (mqttUser != "" && mqttPassword != ""))
    {
        options = new MqttClientOptionsBuilder()
            .WithClientId("mqtt2matrixbot")
            .WithTcpServer(mqttServer, mqttPort)
            .WithCredentials(mqttUser, mqttPassword)
            //.WithTls()
            .WithCleanSession()
            .Build();
    }
    else
    {
        options = new MqttClientOptionsBuilder()
            .WithClientId("mqtt2matrixbot")
            .WithTcpServer(mqttServer, mqttPort)
            //.WithCredentials("username", "password")
            //.WithTls()
            .WithCleanSession()
            .Build();
    }
    var matrixFactory = new MatrixClientFactory();
  
    var client = matrixFactory.Create();
    var matrixNodeAddress = new System.Uri(matrixServer);

    if (matrixusername != null && matrixpassword != null)
    {
        await client.LoginAsync(matrixNodeAddress, matrixusername, matrixpassword, matrixdeviceId);
    }
    else
    {
        Console.WriteLine($"Failed to connect to Matrix-Server. No User or Password provided.");
    }

// connect to Matrix-Server

   // await client.LoginAsync(matrixNodeAddress, matrixusername, matrixpassword, matrixdeviceId);
    ////await client.LoginTokenAsync(matrixNodeAddress, token, deviceId);

    //  await client.JoinTrustedPrivateRoomAsync(roomid);
    // await client.SendMessageAsync(roomid, DateTime.Now.ToString("G"));
    
//client.Start();
    // var x =  await client.SetRoomTopicAsync(roomid, "Hallo Topic Can you see Me ?" + DateTime.Now.ToString("G"));

    // var y =  await client.SetRoomNameAsync(roomid, "Name" + DateTime.Now.ToString("G"));

    // var e = await client.SetRoomAvatarAsync(roomid, closeUrl);


    mqttClient.ApplicationMessageReceivedAsync += async e =>
    {
        
            if (e.ApplicationMessage.Topic == mqttTopicRoomTopic)
            {
                var topic = JsonConvert.DeserializeObject<Topic>(e.ApplicationMessage.ConvertPayloadToString());
                if (topic != null && checkHostName(topic.roomid, matrixHostname))
                {
                    
                    await client.JoinTrustedPrivateRoomAsync(topic.roomid);
                    var t = await client.SetRoomTopicAsync(topic.roomid, topic.topic);
                    Console.WriteLine("Change Topic for room " + topic.roomid + " to " + topic.topic);
                }

              
            }
            else if (e.ApplicationMessage.Topic == mqttTopicRoomName)
            {
                var name = JsonConvert.DeserializeObject<Name>(e.ApplicationMessage.ConvertPayloadToString());
                if(name != null && checkHostName(name.roomid, matrixHostname))
                {
                    await client.JoinTrustedPrivateRoomAsync(name.roomid);
                    var n = await client.SetRoomNameAsync(name.roomid, name.name);
                    Console.WriteLine("Change Name for room " + name.roomid + " to " + name.name);
                }
            }
            else if (e.ApplicationMessage.Topic == mqttTopicRoomAvatar)
            {
                var avatar = JsonConvert.DeserializeObject<Avatar>(e.ApplicationMessage.ConvertPayloadToString());
                if(avatar != null && checkHostName(avatar.roomid, matrixHostname))
                {
                    await client.JoinTrustedPrivateRoomAsync(avatar.roomid);
                    var a = await client.SetRoomAvatarAsync(avatar.roomid, avatar.url);
                    Console.WriteLine("Change Avatar for room " + avatar.roomid + " to " + avatar.url);
                    
                }
            }
            else if (e.ApplicationMessage.Topic == mqttTopicRoomMessage)
            {
                var message = JsonConvert.DeserializeObject<Message>(e.ApplicationMessage.ConvertPayloadToString());
                if(message != null && checkHostName(message.roomid, matrixHostname))
                {
                    await client.JoinTrustedPrivateRoomAsync(message.roomid);
                    var m = await client.SendMessageAsync(message.roomid, message.message);
                    Console.WriteLine("Send Message to room " + message.roomid + " : " + message.message);
                }
            }
            else
            {
                Console.WriteLine("Unknown Topic");
            }
    };

    using (Task<MqttClientConnectResult> connectResult = mqttClient.ConnectAsync(options))
    {
        connectResult.Wait();
        
       

        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(
                f =>
                {
                    f.WithTopic(mqttBaseTopic + "#");
              
                })
            .Build();

        await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);


       
        
        
        if (connectResult.Result.ResultCode == MqttClientConnectResultCode.Success)
        {
            Console.WriteLine("Connected to MQTT broker successfully.");
            
       
        }
        else
        {
            Console.WriteLine($"Failed to connect to MQTT broker. Result code: {connectResult.Result.ResultCode}");
        }


    }


    while (true)
    {
        Thread.Sleep(20);
    }

