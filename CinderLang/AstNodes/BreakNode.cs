using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang.AstNodes
{
    public struct BreakerNode : IAstNode
    {
        public string Name { get; set; }

        public void Generate(IAstNode parent)
        {
            var lp = GenerationHelpers.ScanParents(parent);

            lp.HasBreak = true;
            Program.Builder.BuildBr(lp.ContinueBlock);
        }
    }
}
