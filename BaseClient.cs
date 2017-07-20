using FormsByAir.SDK.Model;
using System.Collections.Generic;

namespace FormsByAir.SDK
{
    public abstract class BaseClient
    {
        public abstract void Deliver(Element document, List<FileDelivery> fileDeliveries);
    }
}
