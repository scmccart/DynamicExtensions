using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicExtensions
{
    interface IDynamicOps
    {
        void MixIn(object mixin, bool overwrite);
        object Send(string methodName, object[] args);
        void Define(string methodName, Delegate method);
    }
}
