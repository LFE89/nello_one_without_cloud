using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NelloBackend.Services.Mqtt;
using NelloBackend.Services.Nello;
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
namespace NelloBackend
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // REMEMBER!!!!
            //
            // It is just a PoC
            // Therefore it can be optimized!

            Console.WriteLine("Nello One - Backend Service");
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();

            try
            {
                var nelloService = serviceProvider.GetService<INelloService>();
                Console.WriteLine("Starting all services...");
                await nelloService.StartServiceAsync();
                Console.WriteLine("Nello One backend started");

                while (true)
                {
                    // Vice versa ;-)
                    Console.WriteLine("----------------------------------");
                    Console.WriteLine("Enter 'quit' to exit the programm.");
                    var input = Console.ReadLine();
                    if (!string.IsNullOrEmpty(input))
                    {
                        if (string.Equals(input.ToLower(), "quit"))
                        {
                            await nelloService.StopServiceAsync();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError("StartServiceAsync error:\n" + ex.ToString());
                Console.ReadLine();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IMqttServer, MqttServer>();
            services.AddSingleton<IMqttClient, MqttClient>();
            services.AddSingleton<INelloService, NelloService>();
            services.AddLogging(configure => configure.AddConsole())
                    .AddTransient<Program>();
        }
    }
}
