using Microsoft.Extensions.Logging;
using NelloBackend.Models;
using NelloBackend.Services.Mqtt;
using System;
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
namespace NelloBackend.Services.Nello
{
    public class NelloService : INelloService
    {
        // Your recorded test message

        private const string TEST_TOPIC_MESSAGE = "{your input}\n";
        private const string DOOR_TOPIC_MESSAGE = "\"\"";

        // Your device id#2 (topic identifier)

        private const string NELLO_TOPICID = "{your input}";

        // Your device id#1 (mqtt client id)

        private const string NELLO_CLIENTID = "{your input}";

        private readonly ILogger<NelloService> _logger = null;

        // Update here your recorded BE_ACK messages
        // Right message received order is required!
        // At least one message is needed

        private readonly string[] _backendACKMessages = { "{your input 1}\n", "{your input 2}\n", "{your input n}\n", "....." };
        
        private int _messageSentCounter = 0;
        private IMqttClient _mqttClient = null;
        private IMqttServer _mqttServer = null;
        private bool _isExpectedResponseReceived = false;
        private bool _isTryingToUnlockDoor = false;
        private bool _mapMessageReceived = false;

        /// <summary>
        /// Just the contructor...
        /// </summary>
        /// <param name="logger">NelloService logger</param>
        /// <param name="mqttClient">MQTT Client</param>
        /// <param name="mqttServer">MQTT Server</param>
        public NelloService(ILogger<NelloService> logger, IMqttClient mqttClient, IMqttServer mqttServer)
        {
            _logger = logger;
            _mqttClient = mqttClient;
            _mqttServer = mqttServer;
            _mqttClient.MqttMessageReceived += new EventHandler(async (s, e) => await MqttClient_MqttMessageReceived(s, e));
        }

        private async Task MqttClient_MqttMessageReceived(object sender, EventArgs e)
        {
            try
            {
                var args = (MqttMessageEventArgs)e;
                if (args.Topic.StartsWith(string.Format("/nello_one/{0}/map", NELLO_TOPICID)))
                {
                    // Connection establishment request

                    if (!_isTryingToUnlockDoor)
                    {
                        _messageSentCounter = 0;
                        
                        await Task.Delay(200);
                        await _mqttClient.SendMessageAsync(string.Format("/nello_one/{0}/test/", NELLO_TOPICID), TEST_TOPIC_MESSAGE);
                    }
                    else
                    {
                        _mapMessageReceived = true;
                    }
                }
                else if (args.Topic.StartsWith(string.Format("/nello_one/{0}/n_online", NELLO_TOPICID)))
                {
                    // Test message accepted, online response received, wating for BE_ACK message

                    if (!_isTryingToUnlockDoor && _messageSentCounter < _backendACKMessages.Length)
                    {
                        await Task.Delay(500);
                        await _mqttClient.SendMessageAsync(string.Format("/nello_one/{0}/BE_ACK/", NELLO_TOPICID), _backendACKMessages[_messageSentCounter]);
                        _messageSentCounter += 1;
                    }
                }
                else if (args.Topic.StartsWith(string.Format("/nello_one/{0}/ring", NELLO_TOPICID)))
                {
                    // Bell ring message

                    Console.WriteLine("RING RING RING");

                    // Do your stuff
                    // await UnlockDoorAsync();
                }
                else if (args.Topic.StartsWith(string.Format("/nello_one/{0}/n_ACK", NELLO_TOPICID)))
                {
                    // E.g. door open command ACK message

                    _isExpectedResponseReceived = true;
                }
                else if (args.Topic.StartsWith(string.Format("/nello_one/{0}/doorcommand", NELLO_TOPICID)))
                {
                    // E.g. door command request received

                    await UnlockDoorAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("MqttClient_MqttMessageReceived error:\n" + ex.ToString());
            }
        }

        /// <summary>
        /// Start full nello offline backend
        /// Including own mqtt broker
        /// </summary>
        /// <returns</returns>
        public async Task StartServiceAsync()
        {
            try
            {
                await _mqttServer.StartServerAsync();
                await _mqttClient.ConnectToBrokerAsync(NELLO_TOPICID);
            }
            catch (Exception ex)
            {
                _logger.LogError("StartServiceAsync error:\n" + ex.ToString());
            }
        }

        /// <summary>
        /// Send unlock door sequence (security bypass)
        /// Forces nello to reconnect
        /// </summary>
        /// <returns></returns>
        public async Task UnlockDoorAsync()
        {
            try
            {
                await Task.Run(async() =>  {

                    // Remove nello's session

                    await _mqttServer.DisconnectClientAsync(NELLO_CLIENTID);
                    await Task.Delay(250);

                    _isTryingToUnlockDoor = true;
                    _isExpectedResponseReceived = false;
                    int counter = 0;

                    // Todo: optimize: use e.g. event reset
                    // Blabla...
                    // Wait for nello's map message

                    while(!_mapMessageReceived)
                    {
                        await Task.Delay(200);
                    }

                    while (!_isExpectedResponseReceived && counter < 30)
                    {
                        // Send magic payload to test topic

                        await _mqttClient.SendMessageAsync(string.Format("/nello_one/{0}/test/", NELLO_TOPICID), TEST_TOPIC_MESSAGE);

                        // Delay - in my case - necessary
                        // Give nello some time to prepare for the next message
                        // Maybe you need to increase your delay value

                        await Task.Delay(500);

                        // Send magic payload to door topic

                        await _mqttClient.SendMessageAsync(string.Format("/nello_one/{0}/door/", NELLO_TOPICID), DOOR_TOPIC_MESSAGE);

                        // Delay included to have some time to react to nello responses

                        await Task.Delay(550);
                        counter += 1;
                    }
                    
                    _mapMessageReceived = false;
                    _isTryingToUnlockDoor = false;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError("UnlockDoorAsync error:\n" + ex.ToString());
                _isTryingToUnlockDoor = false;
                _isExpectedResponseReceived = false;
                _mapMessageReceived = false;
            }
        }

        /// <summary>
        /// Stop the entire service
        /// </summary>
        /// <returns></returns>
        public async Task StopServiceAsync()
        {
            try
            {
                await _mqttClient.DisconnectAsync();
                await _mqttServer.StopServerAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError("StopServiceAsync error:\n" + ex.ToString());
            }
        }
    }
}
