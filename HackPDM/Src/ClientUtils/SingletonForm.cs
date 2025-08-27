using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using HackPDM.Extensions.Control;

namespace HackPDM.ClientUtils
{
    public interface ISingletonForm<T>
        where T : Form, new()
    {
        public static abstract T? Singleton { get; set; }   
    }
}
