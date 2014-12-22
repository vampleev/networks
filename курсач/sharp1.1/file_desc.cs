using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace networks
{
    class file_desc
    {
        public string file_name;
        public long file_size;
        public file_desc(string name, long size)
        {
            file_name = name;
            file_size = size;
        }
    }
}
