using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Chutzpah.Facts.Library
{
    public class ChutzpahContainerFacts
    {
        [Fact]
        public void ContainerIsValid()
        {
            ChutzpahContainer.Current.AssertConfigurationIsValid();
        }
    }

}
