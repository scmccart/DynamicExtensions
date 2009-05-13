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
            {
                ops.MixIn(mixin, overwrite);
                return target;
            }
            else
            {
                return target.Transmute().MixIn(mixin, overwrite);
            }
        }

        public static object Send(this object target, string methodName, object[] args)
        {
            var ops = target as IDynamicOps;

            if (ops != null)
                return ops.Send(methodName, args);
            else
                throw new InvalidOperationException("This is not a Dynamic!");
        }

        public static object Define(this object target, string methodName, Delegate method) {
            var ops = target as IDynamicOps;

            if (ops != null)
            {
                ops.Define(methodName, method);
                return target;
            }
            else
            {
                return target.Transmute().Define(methodName, method);
            }
        }
    }
}
