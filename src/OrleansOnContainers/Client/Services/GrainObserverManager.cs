using GrainInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Services;
internal class GrainObserverManager : IGrainObserverManager
{
    public void Subscribe(IChatObserver observer, int grainId)
    {
        throw new NotImplementedException();
    }

    public void Unsubscribe(IChatObserver observer, int grainId)
    {
        throw new NotImplementedException();
    }
}
