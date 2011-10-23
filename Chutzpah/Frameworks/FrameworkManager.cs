namespace Chutzpah.Frameworks
{
    using System;
    using System.Collections.Generic;

    public class FrameworkManager : IFrameworkManager
    {
        private static readonly IDictionary<Framework, IFrameworkDefinition> frameworks;

        static FrameworkManager()
        {
            frameworks = new Dictionary<Framework, IFrameworkDefinition>()
            {
                { Framework.QUnit, new QUnitDefinition() },
                { Framework.Jasmine, new JasmineDefinition() }
            };
        }

        public ICollection<Framework> Keys
        {
            get
            {
                return frameworks.Keys;
            }
        }

        public ICollection<IFrameworkDefinition> Values
        {
            get
            {
                return frameworks.Values;
            }
        }

        public IFrameworkDefinition this[Framework key]
        {
            get
            {
                if (key == Framework.Unknown)
                {
                    throw new NotSupportedException(key.ToString());
                }

                return frameworks[key];
            }
        }

        public bool TryDetectFramework(string content, out IFrameworkDefinition definition)
        {
            return this.TryDetectFramework(content, false, out definition) || this.TryDetectFramework(content, true, out definition);
        }

        private bool TryDetectFramework(string content, bool bestGuess, out IFrameworkDefinition definition)
        {
            foreach (var framework in frameworks.Values)
            {
                if (framework.FileUsesFramework(content, bestGuess))
                {
                    definition = framework;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
