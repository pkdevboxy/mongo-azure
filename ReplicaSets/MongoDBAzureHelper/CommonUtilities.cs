﻿/*
 * Copyright 2010-2012 10gen Inc.
 * file : CommonUtilities.cs
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

namespace MongoDB.Azure.Common
{
    using System;

    public static class CommonUtilities
    {

        public static int ParseNodeInstanceId(string id)
        {
            int instanceId = int.Parse(id.Substring(id.LastIndexOf("_") + 1));
            return instanceId;
        }

        public static string GetNodeAlias(string replicaSetName, int instanceId)
        {
            var alias = string.Format("{0}_{1}", replicaSetName, instanceId);
            return alias;
        }

    }
}
