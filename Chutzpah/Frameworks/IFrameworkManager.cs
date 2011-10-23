namespace Chutzpah.Frameworks
{
    using System.Collections.Generic;

    public enum Framework
    {
        Unknown,
        QUnit,
        Jasmine
    }

    public interface IFrameworkManager
    {
        ICollection<Framework> Keys { get; }

        ICollection<IFrameworkDefinition> Values { get; }

        IFrameworkDefinition this[Framework key] { get; }

        bool TryDetectFramework(string content, out IFrameworkDefinition definition);
    }
}
