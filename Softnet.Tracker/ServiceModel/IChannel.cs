/*
*   Copyright 2023 Robert Koifman
*   
*   Licensed under the Apache License, Version 2.0 (the "License");
*   you may not use this file except in compliance with the License.
*   You may obtain a copy of the License at
*
*   http://www.apache.org/licenses/LICENSE-2.0
*
*   Unless required by applicable law or agreed to in writing, software
*   distributed under the License is distributed on an "AS IS" BASIS,
*   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
*   See the License for the specific language governing permissions and
*   limitations under the License.
*/

using System;
using System.Collections.Generic;

namespace Softnet.Tracker.ServiceModel
{
    public interface IChannel : Softnet.ServerKit.Monitorable
    {
        void Init(ServiceInstaller serviceInstaller, Action establishedCallback);
        System.Net.Sockets.AddressFamily GetAddressFamily();
        void SetCompletedCallback(Action callback);
        void RegisterModule(int moduleId, Action<byte[]> messageReceivedCallback);
        void RemoveModule(int moduleId);
        void Send(Softnet.ServerKit.SoftnetMessage message);
        void Start();
        void Shutdown(int errorCode);
        void Close();
    }
}
