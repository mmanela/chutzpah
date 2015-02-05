using System;
using System.Linq;
using System.Text.RegularExpressions;
using Chutzpah.FrameworkDefinitions;
using Chutzpah.Models;
using Chutzpah.Wrappers;

namespace Chutzpah.FileProcessors
{
    public class MochaInterfaceDetectionProcessor : IMochaReferencedFileProcessor
    {

        public MochaInterfaceDetectionProcessor()
        {

        }

        public void Process(IFrameworkDefinition frameworkDefinition, ReferencedFile referencedFile, string testFileText, ChutzpahTestSettingsFile settings)
        {
            referencedFile.FrameworkReplacements = frameworkDefinition.GetFrameworkReplacements(settings, referencedFile.Path, testFileText);
        }

    }
}
