using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Models
{
    public class VSTSTestCase : TestCase, IEquatable<VSTSTestCase>
    {

        public Guid Id { get; private set; }
        public Guid ExecutionId { get; private set; }

        public Exception exception { get; set; }

        public bool Passed { get; set; }

        public VSTSTestCase()
        {
            this.Id = Guid.NewGuid();
            this.ExecutionId = Guid.NewGuid();
        }

        public VSTSTestCase UpdateWith(TestCase that)
        {
            this.Column = that.Column;
            this.HtmlTestFile = that.HtmlTestFile;
            this.InputTestFile = that.InputTestFile;
            this.Line = that.Line;
            this.ModuleName = that.ModuleName;
            this.Passed = that.ResultsAllPassed;
            this.TestName = that.TestName;
            this.TestResults = that.TestResults;
            this.TimeTaken = that.TimeTaken;
            this.Skipped = that.Skipped;
            return this;
        }

        public bool Equals(VSTSTestCase other)
        {
            return other.TestName.Equals(this.TestName) &&
                (String.IsNullOrEmpty(this.ModuleName) || (this.ModuleName.Equals(other.ModuleName)));
        }
    }
}
