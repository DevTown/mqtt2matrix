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

// MATRIXSERVER muss eine gültige absolute URL sein, sonst crasht der spätere
// new Uri(matrixServer) beim Start. Früh und mit klarer Meldung abbrechen.
if (string.IsNullOrWhiteSpace(matrixServer)
    || !Uri.TryCreate(matrixServer, UriKind.Absolute, out _))
{
    Console.WriteLine("MATRIXSERVER fehlt oder ist keine gültige absolute URL (z.B. https://matrix.example.org). Beende.");
    Environment.Exit(1);
    return;
}


var closeUrl = "mxc://matrix.c3re.de/YjVhJIHNrbEXmZmwAgplvNCs";
var openUrl = "mxc://matrix.c3re.de/IJHumgqYFRjmRLqRfBnYgLaJ";
var roomid2 = "!hhLRgoyFSLKlgPhgHF:matrix.c3re.de"; //Jarvis Test Room


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
    var matrixNodeAddress = new System.Uri(matrixServer!);

    if (!string.IsNullOrWhiteSpace(matrixusername) && !string.IsNullOrWhiteSpace(matrixpassword))
    {
        try
        {
            await client.LoginAsync(matrixNodeAddress, matrixusername, matrixpassword, matrixdeviceId);
            Console.WriteLine("Logged in to Matrix-Server successfully.");
        }
        catch (Exception ex)
        {
            // Ohne erfolgreichen Login kann die Bridge nichts ausrichten. Lieber hart
            // beenden (Docker-Restart-Policy übernimmt den Retry) als still weiterlaufen
            // und jede MQTT-Nachricht ins Leere fehlschlagen lassen.
            Console.WriteLine($"Failed to log in to Matrix-Server: {ex.Message}. Beende.");
            Environment.Exit(1);
        }
    }
    else
    {
        Console.WriteLine("Failed to connect to Matrix-Server. No User or Password provided. Beende.");
        Environment.Exit(1);
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
        try
        {
            if (e.ApplicationMessage.Topic == mqttTopicRoomTopic)
            {
                var topic = JsonConvert.DeserializeObject<Topic>(e.ApplicationMessage.ConvertPayloadToString());
                if (topic != null)
                {
                    
                    await client.JoinTrustedPrivateRoomAsync(topic.roomid);
                    var t = await client.SetRoomTopicAsync(topic.roomid, topic.topic);
                    Console.WriteLine("Change Topic for room " + topic.roomid + " to " + topic.topic);
                }

              
            }
            else if (e.ApplicationMessage.Topic == mqttTopicRoomName)
            {
                var name = JsonConvert.DeserializeObject<Name>(e.ApplicationMessage.ConvertPayloadToString());
                if(name != null)
                {
                    await client.JoinTrustedPrivateRoomAsync(name.roomid);
                    var n = await client.SetRoomNameAsync(name.roomid, name.name);
                    Console.WriteLine("Change Name for room " + name.roomid + " to " + name.name);
                }
            }
            else if (e.ApplicationMessage.Topic == mqttTopicRoomAvatar)
            {
                var avatar = JsonConvert.DeserializeObject<Avatar>(e.ApplicationMessage.ConvertPayloadToString());
                if(avatar != null)
                {
                    await client.JoinTrustedPrivateRoomAsync(avatar.roomid);
                    var a = await client.SetRoomAvatarAsync(avatar.roomid, avatar.url);
                    Console.WriteLine("Change Avatar for room " + avatar.roomid + " to " + avatar.url);
                    
                }
            }
            else if (e.ApplicationMessage.Topic == mqttTopicRoomMessage)
            {
                var message = JsonConvert.DeserializeObject<Message>(e.ApplicationMessage.ConvertPayloadToString());
                if(message != null)
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
        }
        catch (Exception ex)
        {
            // Einzelne fehlerhafte Nachricht (kaputtes JSON, Matrix-API-Fehler) darf
            // weder den Handler noch den Prozess killen.
            Console.WriteLine($"Error while handling message on topic '{e.ApplicationMessage.Topic}': {ex.Message}");
        }
    };

    var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
        .WithTopicFilter(
            f =>
            {
                f.WithTopic(mqttBaseTopic + "#");
            })
        .Build();

    // Verbindet (mit Backoff-Retry, bis es klappt) und abonniert anschließend.
    // Wird sowohl beim Start als auch nach einem Disconnect (Reconnect) aufgerufen.
    async Task ConnectAndSubscribeAsync()
    {
        var delaySeconds = 2;
        while (true)
        {
            try
            {
                var result = await mqttClient.ConnectAsync(options, CancellationToken.None);
                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
                    Console.WriteLine("Connected to MQTT broker successfully.");
                    return;
                }

                Console.WriteLine($"Failed to connect to MQTT broker. Result code: {result.ResultCode}. Retrying in {delaySeconds}s...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT connection error: {ex.Message}. Retrying in {delaySeconds}s...");
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            delaySeconds = Math.Min(delaySeconds * 2, 30); // exponentielles Backoff, gedeckelt auf 30s
        }
    }

    // Laufzeit-Reconnect: bei Verbindungsverlust nach kurzer Pause neu verbinden + abonnieren.
    mqttClient.DisconnectedAsync += async e =>
    {
        // Ein fehlgeschlagener ConnectAsync feuert dieses Event ebenfalls. Um diese Fälle
        // kümmert sich bereits die Retry-Schleife in ConnectAndSubscribeAsync. Hier nur auf
        // den Verlust einer zuvor bestehenden Verbindung reagieren – sonst startet jeder
        // Fehlversuch eine weitere Reconnect-Schleife (Connect-Storm gegen den Broker).
        if (!e.ClientWasConnected)
            return;

        Console.WriteLine($"MQTT disconnected: {e.Reason}. Reconnecting...");
        await Task.Delay(TimeSpan.FromSeconds(5));
        await ConnectAndSubscribeAsync();
    };

    await ConnectAndSubscribeAsync();


    while (true)
    {
        Thread.Sleep(20);
    }

