using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADP.Portal.Core.Git.Entities
{
    public class GitRepo
    {
        public required string RepoName { get; set; }
        public required string BranchName { get; set; }
        public required string Organisation { get; set; }
    }
}
