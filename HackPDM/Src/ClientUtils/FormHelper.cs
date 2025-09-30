using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.UI.Dispatching;

namespace HackPDM.ClientUtils;

internal static class FormHelper
{
    internal async static Task ExecuteUI(this DispatcherQueue dispatcher, Func<Task> function)
    {
        if (dispatcher.HasThreadAccess)
        {
            await function();
        }
        else
        {
            dispatcher.TryEnqueue(async ()=>await function());
        }
    }
    internal static void ExecuteUI(this DispatcherQueue dispatcher, Action function)
    {
        if (dispatcher.HasThreadAccess)
        {
            function();
        }
        else
        {
            dispatcher.TryEnqueue(()=>function());
        }
    }
}