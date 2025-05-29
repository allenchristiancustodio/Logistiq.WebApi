using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Infrastructure.Settings
{
    public class KindeSettings
    {
        public string Domain { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
    }
}
