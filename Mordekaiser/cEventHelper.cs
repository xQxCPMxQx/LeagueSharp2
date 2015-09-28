using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace Mordekaiser
{
    using System.Linq;

    public static class cEventHelper
    {
        private static readonly Dictionary<Type, List<FieldInfo>> DicEventFieldInfos =
            new Dictionary<Type, List<FieldInfo>>();

        private static BindingFlags AllBindings
        {
            get
            {
                return BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                       | BindingFlags.Static;
            }
        }

        private static List<FieldInfo> GetTypeEventFields(Type t)
        {
            if (DicEventFieldInfos.ContainsKey(t)) return DicEventFieldInfos[t];

            List<FieldInfo> lst = new List<FieldInfo>();
            BuildEventFields(t, lst);
            DicEventFieldInfos.Add(t, lst);
            return lst;
        }

        private static void BuildEventFields(Type t, List<FieldInfo> lst)
        {
            lst.AddRange(
                from ei in t.GetEvents(AllBindings)
                let dt = ei.DeclaringType
                select dt.GetField(ei.Name, AllBindings)
                into fi
                where fi != null
                select fi);
        }

        private static EventHandlerList GetStaticEventHandlerList(Type t, object obj)
        {
            MethodInfo mi = t.GetMethod("get_Events", AllBindings);
            return (EventHandlerList)mi.Invoke(obj, new object[] { });
        }

        public static void RemoveAllEventHandlers(object obj)
        {
            RemoveEventHandler(obj, "");
        }

        public static void RemoveEventHandler(object obj, string EventName)
        {
            if (obj == null) return;

            Type t = obj.GetType();
            List<FieldInfo> eventFields = GetTypeEventFields(t);
            EventHandlerList staticEventHandlers = null;

            foreach (
                FieldInfo fi in
                    eventFields.Where(
                        fi =>
                        EventName == "" || String.Compare(EventName, fi.Name, StringComparison.OrdinalIgnoreCase) == 0))
            {
                if (fi.IsStatic)
                {
                    if (staticEventHandlers == null) staticEventHandlers = GetStaticEventHandlerList(t, obj);

                    object idx = fi.GetValue(obj);
                    Delegate eh = staticEventHandlers[idx];
                    if (eh == null) continue;

                    Delegate[] dels = eh.GetInvocationList();

                    EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                    foreach (Delegate del in dels) ei.RemoveEventHandler(obj, del);
                }
                else
                {
                    EventInfo ei = t.GetEvent(fi.Name, AllBindings);
                    if (ei != null)
                    {
                        object val = fi.GetValue(obj);
                        Delegate mdel = (val as Delegate);
                        if (mdel == null)
                        {
                            continue;
                        }
                        foreach (var del in mdel.GetInvocationList()) ei.RemoveEventHandler(obj, del);
                    }
                }
            }
        }
    }
}
