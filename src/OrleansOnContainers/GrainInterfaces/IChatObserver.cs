﻿namespace GrainInterfaces;

public interface IChatObserver : IGrainObserver
{
    Task ReceiveMessage(IMessage message);
}
