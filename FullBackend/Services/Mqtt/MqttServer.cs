using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using System;
using System.Linq;
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
    public class MqttServer : IMqttServer
    {
        private readonly ILogger<MqttServer> _logger = null;
        private MQTTnet.Server.IMqttServer _mqttServer = null;

        public MqttServer(ILogger<MqttServer> logger)
        {
            _logger = logger;
        }

        public async Task DisconnectClientAsync(string clientId)
        {
            try
            {
                var sessions = await _mqttServer.GetSessionStatusAsync();
                if(sessions != null)
                {
                    var nelloSession = sessions.FirstOrDefault(x => x.ClientId == clientId);
                    if(nelloSession != null)
                    {
                        await nelloSession.DeleteAsync();
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError("DisconnectClientAsync error:\n" + ex.ToString());
            }
        }

        public async Task StartServerAsync()
        {
            try
            {
                var optionsBuilder = new MqttServerOptionsBuilder()
                .WithConnectionBacklog(100)
                .WithDefaultEndpointPort(1883);

                _mqttServer = new MqttFactory().CreateMqttServer();
                await _mqttServer.StartAsync(optionsBuilder.Build());
            }
            catch (Exception ex)
            {
                _logger.LogError("StartServerAsync error:\n" + ex.ToString());
                Console.ReadLine();
            }
        }

        public async Task StopServerAsync()
        {
            try
            {
                await _mqttServer.StopAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("StartServerAsync error:\n" + ex.ToString());
            }
        }
    }
}