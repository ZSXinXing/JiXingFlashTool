using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JXAdbCore.Receivers
{
    public abstract class MultiLineReceiver : IShellOutputReceiver
    {
        public MultiLineReceiver() => Lines = new List<string>();
        public bool TrimLines { get; set; }
        public bool ParsesErrors { get; protected set; }
        protected ICollection<string> Lines { get; set; }
        public void AddOutput(string line) => Lines.Add(line);
        public void Flush()
        {
            if (Lines.Count > 0)
            {
                // send it for final processing
                ProcessNewLines(Lines);
                Lines.Clear();
            }

            Done();
        }
        protected void Done()
        {
        }

        protected abstract void ProcessNewLines(IEnumerable<string> lines);
    }
}
