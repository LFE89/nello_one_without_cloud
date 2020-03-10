using System;
using System.Threading.Tasks;

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
namespace NelloOneTest.Services
{
    interface IMqttService
    {
        public Task SendMessageAsync(string topicName, string message);
        public Task ConnectToBrokerAsync();
        public Task SubscribeToTopicAsync(string topicName);
        public event EventHandler AckOnlineSequenceReceived;
    }
}