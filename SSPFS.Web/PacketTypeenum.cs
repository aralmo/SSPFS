using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SSPFS.Web
{
    public enum PacketTypeEnum
    {
        ListFiles = 1,
        DownloadFile = 4,
        UploadFile= 5,
    }
}
