﻿/*
 * Copyright 2010-2012 10gen Inc.
 * file : WinHostsUpdater.cs
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace MongoDB.WindowsAzure.InstanceMaintainer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;

    using Microsoft.WindowsAzure;
    using Microsoft.WindowsAzure.ServiceRuntime;

    using MongoDB.WindowsAzure.Common;

    public class WinHostsUpdater
    {
        private static string WinHostFile = Environment.ExpandEnvironmentVariables("%windir%\\System32\\drivers\\etc\\hosts");
        private const string WinHostFileHeader = @"
#
# This is a HOSTS file generated by the MongoDB Worker Role.
#

    ";
        private const string HostString = "{0}\t\t{1}\n";

        private static AvailableNodes availableNodes = null;
        private static int defaultTimeIntervalBetweenUpdates = 15; // in seconds

        static void Main(string[] args)
        {
            try
            {
                Trace.TraceInformation("Starting host file updater");
                if ((!RoleEnvironment.IsAvailable) || (RoleEnvironment.IsEmulated))
                {
                    Trace.TraceInformation("Not in a deployed environment. Nothing to do. Exiting");
                    return;
                }

                int timeInSeconds = 0;
                if (args.Length > 0)
                {
                    if (!Int32.TryParse(args[0], out timeInSeconds))
                    {
                        timeInSeconds = defaultTimeIntervalBetweenUpdates;
                    }
                }

                CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) =>
                {
                    configSetter(RoleEnvironment.GetConfigurationSettingValue(configName));
                });

                var rsName = ConnectionUtilities.GetReplicaSetName();

                Trace.TraceInformation("Replica set name is {0}", rsName);

                while (true)
                {
                    UpdateWinhostsFile(rsName);
                    Thread.Sleep(timeInSeconds * 1000);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Exception {0} and stack trace{1} and inner exception {2}", e.Message, e.StackTrace, e.InnerException);
            }
            finally
            {
                Trace.Flush();
            }
        }

        private static void UpdateWinhostsFile(string rsName)
        {
            var currentNodes = GetAvailableNodes(rsName);
            if (!currentNodes.Equals(availableNodes))
            {
                Trace.TraceInformation("Node information changed from {0} to {1}",
                    (availableNodes == null) ? null : availableNodes.ToString(),
                    currentNodes);
                availableNodes = currentNodes;
                WriteWinhostsFile();
            }
        }

        private static void WriteWinhostsFile()
        {
            var hostFile = new StringBuilder(WinHostFileHeader);
            hostFile.AppendLine();
            foreach (var node in availableNodes)
            {
                hostFile.AppendFormat(HostString, node.IpAddress, node.Alias);
            }
            Trace.TraceInformation("Writing {0} to hosts file", hostFile.ToString());
            using (var writer = new StreamWriter(WinHostFile))
            {
                writer.WriteLine(hostFile.ToString());
            }
        }

        private static AvailableNodes GetAvailableNodes(string rsName)
        {
            var nodes = new AvailableNodes();
            foreach (var instance in RoleEnvironment.Roles[Constants.MongoDBWorkerRoleName].Instances)
            {
                nodes.Add(new NodeAlias()
                {
                    Alias = ConnectionUtilities.GetNodeAlias(rsName, ConnectionUtilities.ParseNodeInstanceId(instance.Id)),
                    IpAddress = instance.InstanceEndpoints[Constants.MongodPortSetting].IPEndpoint.Address.ToString()
                });
            }
            return nodes;
        }

    }

}
