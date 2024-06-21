using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Git.Entities
{
    public class UserGroupMembership
    {
        public List<string> TechUser { get; set; } = [];

        public List<string> NontechUser { get; set; } = [];

        public List<string> Admin { get; set; } = [];
    }
}
