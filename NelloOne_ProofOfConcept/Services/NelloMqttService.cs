using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Text;
using System.Threading.Tasks;

namespace NelloOneTest.Services
{
    /// <summary>
    /// Copyright (C) 2020 Lars D. Feicho
    /// 
    /// Proof of concept
    /// Nello (nello.io) open door without cloud (security issue)
    ///
    /// This program is free software: you can redistribute it and/or modify
    /// it under the terms of the GNU General Public License as published by
    /// the Free Software Foundation, either version 3 of the License, or
    /// (at your option) any later version.
    /// This program is distributed in the hope that it will be useful,
    /// but WITHOUT ANY WARRANTY; without even the implied warranty of
    /// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    /// GNU General Public License for more details.
    /// You should have received a copy of the GNU General Public License
    /// along with this program.If not, see<http://www.gnu.org/licenses/>.
    /// </summary>
    public class NelloMqttService : IMqttService
    {
        private const string SERVER_ADDRESS = "192.168.8.3";
        private const int SERVER_PORT = 1883;
        private const string DEVICE_ID = "INSERT YOUR DEVICE ID 2 HERE";
        private const string TOPIC_PREFIX = "/nello_one/";

        private readonly ILogger<NelloMqttService> _logger = null;

        private IMqttClient _client;
        private string[] _mqttListeningTopics = new string[] {"n_online/", "n_ACK/" };

        public event EventHandler AckOnlineSequenceReceived = delegate { };


        public NelloMqttService(ILogger<NelloMqttService> logger)
        {
            logger = _logger;

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
        }

        public async Task ConnectToBrokerAsync()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
             .WithTcpServer(SERVER_ADDRESS, SERVER_PORT)
             // .WithCredentials(USERNAME, PASSWORD)
             .Build();

                _client.UseDisconnectedHandler(async e =>
                {
                    try
                    {
                        await _client.ReconnectAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("UseDisconnectedHandler error:\n" + ex.ToString());
                    }
                });

                _client.UseConnectedHandler(async e =>
                {
                    try
                    {
                        foreach(var topic in _mqttListeningTopics)
                        {
                            await SubscribeToTopicAsync(string.Format("{0}{1}/{2}", TOPIC_PREFIX, DEVICE_ID, topic));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("UseConnectedHandler error:\n" + ex.ToString());
                    }
                });

                _client.UseApplicationMessageReceivedHandler(e =>
                {
                    try
                    {
                        if (e.ApplicationMessage.Topic.StartsWith("/nello_one"))
                        {
                            if (e.ApplicationMessage.Topic.StartsWith(string.Format("{0}{1}/{2}", TOPIC_PREFIX, DEVICE_ID, _mqttListeningTopics[1])))
                            {
                                // Message received 
                                // Do stuff here
                                OnAckOnlineSequenceReceived(new EventArgs());
                            }

                            Console.WriteLine(string.Format("{0}: {1}", e.ApplicationMessage.Topic, Encoding.UTF8.GetString(e.ApplicationMessage.Payload)));
  
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("UseApplicationMessageReceivedHandler error:\n" + ex.ToString());
                    }
                });

                await _client.ConnectAsync(options);
            }
            catch (Exception ex)
            {
                _logger.LogError("ConnectToBrokerAsync error:\n" + ex.ToString());
            }
        }

        public async Task SendMessageAsync(string topicName, string message)
        {
            try
            {
                await _client.PublishAsync(topicName, message, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce ,false);
            }
            catch (Exception ex)
            {
                _logger.LogError("SendMessageAsync error:\n" + ex.ToString());
            }
        }

        public async Task SubscribeToTopicAsync(string topicName)
        {
            try
            {
                await _client.SubscribeAsync(new TopicFilterBuilder().WithTopic(topicName).Build());
            }
            catch (Exception ex)
            {
                _logger.LogError("SubscribeToTopicAsync error:\n" + ex.ToString());
            }
        }

        public void OnAckOnlineSequenceReceived(EventArgs eventArgs)
        {
            try
            {
                AckOnlineSequenceReceived?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError("OnAckOnlineSequenceReceived error:\n" + ex.ToString());

            }
        }
    }
}
