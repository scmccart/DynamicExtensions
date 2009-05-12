using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicExtensions
{
    public static class DynamicExtensions
    {
        public static object Transmute(this object obj)
        {
            return new Dynamic(obj).GetTransparentProxy();
        }

        public static object MixIn(this object target, object mixin)
        {
            return target.MixIn(mixin, false);
        }

        public static object MixIn(this object target, object mixin, bool overwrite)
        {
            var ops = target as IDynamicOps;
            if (ops != null)
                ops.MixIn(mixin, overwrite);
            else
                throw new InvalidOperationException("This is not a Dynamic!");

            return target;
        }

        public static object Send(this object target, string methodName, object[] args)
        {
            var ops = target as IDynamicOps;

            if (ops != null)
                return ops.Send(methodName, args);
            else
                throw new InvalidOperationException("This is not a Dynamic!");
        }
    }
}
