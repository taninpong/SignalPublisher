// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.SignalR.Samples.Management
{
    public class MessagePublisher : Hub
    {
        private const string Target = "Target";
        private const string HubName = "ManagementSampleHub";
        private readonly string _connectionString;
        private readonly ServiceTransportType _serviceTransportType;
        private IServiceHubContext _hubContext;

        public MessagePublisher(string connectionString, ServiceTransportType serviceTransportType)
        {
            _connectionString = connectionString;
            _serviceTransportType = serviceTransportType;
        }

        public async Task InitAsync()
        {
            var serviceManager = new ServiceManagerBuilder().WithOptions(option =>
            {
                option.ConnectionString = _connectionString;
                option.ServiceTransportType = _serviceTransportType;
            }).Build();
            _hubContext = await serviceManager.CreateHubContextAsync(HubName, new LoggerFactory());
        }

        public Task ManageUserGroup(string command, string userId, string groupName)
        {
            switch (command)
            {
                case "add":
                    return _hubContext.UserGroups.AddToGroupAsync(userId, groupName);
                case "remove":
                    return _hubContext.UserGroups.RemoveFromGroupAsync(userId, groupName);
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return Task.CompletedTask;
            }
        }

        public async Task<bool> SendMessages(string command, string receiver, string message)
        {
            var newmess = DateTime.UtcNow.ToString("HH: mm:ss.fff tt") + "|" + message;
            switch (command)
            {
                case "broadcast":
                    await _hubContext.Clients.All.SendAsync(Target, message);
                    return true;
                case "user":
                    {
                        var userId = receiver;
                        var table = GetConnectionTable2();
                        List<EmployeeEntity> lists = table.CreateQuery<EmployeeEntity>()
                            .Where(it => it.PartitionKey == userId).ToList();

                        //await _hubContext.Clients.Users("abcd").SendAsync(Target, "Hi");

                        foreach (var item in lists)
                        {
                            await _hubContext.Clients.Users(item.PartitionKey).SendAsync(Target, message);
                        }
                        return lists.Any();
                        //return Task.CompletedTask;
                        //case "users":
                        //    var userIds = receiver.Split(',');
                        //    return _hubContext.Clients.Users(userIds).SendAsync(Target, message);
                        //case "group":
                        //    var groupName = receiver;
                        //    return _hubContext.Clients.Group(groupName).SendAsync(Target, message);
                        //case "groups":
                        //    var groupNames = receiver.Split(',');
                        //    return _hubContext.Clients.Groups(groupNames).SendAsync(Target, message);
                    }
                default:
                    Console.WriteLine($"Can't recognize command {command}");
                    return false;
            }
        }

        public Task DisposeAsync() => _hubContext?.DisposeAsync();

        private CloudTable GetConnectionTable2()
        {
            var storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=signalab;AccountKey=xbad3br/3o0AglWZ4iM1WdepVOlm9CSoMRmDbUlvFYmmUmJTlHF2hxqvsnC99fELsLvhQE1YzAi1x3mLOh9Yhg==;EndpointSuffix=core.windows.net");
            var tableClient = storageAccount.CreateCloudTableClient();
            return tableClient.GetTableReference("demotable2");
        }

    }
}