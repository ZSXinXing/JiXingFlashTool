using JiXingFlashTool.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiXingFlashTool.EventArg
{
    public class EthernetSNEventArgs : EventArgs
    {
        public EthernetSNModel ethernetSNModel { get; set; }
        public EthernetSNEventArgs(EthernetSNModel ethernetSN) => this.ethernetSNModel = ethernetSN;
    }
}
