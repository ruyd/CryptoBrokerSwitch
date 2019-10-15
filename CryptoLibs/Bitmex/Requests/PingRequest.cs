﻿using Bitmex.Client.Websocket.Messages;

namespace Bitmex.Client.Websocket.Requests
{
    public class PingRequest : RequestBase
    {
        public override MessageType Operation => MessageType.Ping;

        public override bool IsRaw => true;
    }
}
