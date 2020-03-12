using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using NelloBackend.Models;
using System;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Copyright (C) 2020 Lars D. Feicho
/// 
/// Proof of concept - Nello Backend Services
/// Full impl. of a offline nello.io / sclak.com backend
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
namespace NelloBackend.Services.Mqtt
{
    public class MqttClient : IMqttClient
    {
        // Broker ip address

        private const string SERVER_ADDRESS = "192.168.8.3";

        // Broker port

        private const int SERVER_PORT = 1883;
        private const string TOPIC_PREFIX = "/nello_one/";
        private readonly ILogger<MqttClient> _logger = null;
        private readonly string[] _mqttListeningTopics = new string[] { "map/", "n_online/", "n_ACK/", "ring/", "doorcommand/" };

        private MQTTnet.Client.IMqttClient _client;
        private string _nelloTopicId = null;

        public event EventHandler MqttMessageReceived;

        public MqttClient(ILogger<MqttClient> logger)
        {
            _logger = logger;

            var factory = new MqttFactory();
            _client = factory.CreateMqttClient();
        }

        public async Task ConnectToBrokerAsync(string nelloTopicId)
        {
            try
            {
                _nelloTopicId = nelloTopicId;

                var options = new MqttClientOptionsBuilder()
             .WithTcpServer(SERVER_ADDRESS, SERVER_PORT)
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
                        foreach (var topic in _mqttListeningTopics)
                        {
                            await SubscribeToTopicAsync(string.Format("{0}{1}/{2}", TOPIC_PREFIX, _nelloTopicId, topic));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("UseDisconnectedHandler error:\n" + ex.ToString());
                    }

                });

                _client.UseApplicationMessageReceivedHandler(e =>
                {
                    try
                    {
                        if (e.ApplicationMessage.Topic.StartsWith("/nello_one"))
                        {
                            var args = new MqttMessageEventArgs();
                            args.ClientId = e.ClientId;
                            args.Message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                            args.Topic = e.ApplicationMessage.Topic;
                            MqttMessageReceived?.Invoke(this, args);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("UseDisconnectedHandler error:\n" + ex.ToString());
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
                await _client.PublishAsync(topicName, message, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce, false);
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

        public async Task DisconnectAsync()
        {
            try
            {
                await _client.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("DisconnectAsync error:\n" + ex.ToString());
            }
        }
    }
}