using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    class PseudoUniqueFile
    {
        public string path;
        public long filesize;

        public PseudoUniqueFile (string pathArg, long filesizeArg)
        {
            path = pathArg;
            filesize = filesizeArg;
        }
    }
}
