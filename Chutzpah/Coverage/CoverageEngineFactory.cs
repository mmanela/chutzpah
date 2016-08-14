using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Wrappers;

namespace Chutzpah.Coverage
{
    public interface ICoverageEngineFactory
    {
        ICoverageEngine CreateCoverageEngine();
    }

    public class CoverageEngineFactory : ICoverageEngineFactory
    {
        private readonly IFileSystemWrapper fileSystem;
        private readonly IJsonSerializer jsonSerializer;
        private readonly ILineCoverageMapper lineCoverageMapper;
        readonly IUrlBuilder urlBuilder;

        public CoverageEngineFactory(IJsonSerializer jsonSerializer, IFileSystemWrapper fileSystem, ILineCoverageMapper lineCoverageMapper, IUrlBuilder urlBuilder)
        {
            this.urlBuilder = urlBuilder;
            this.jsonSerializer = jsonSerializer;
            this.fileSystem = fileSystem;
            this.lineCoverageMapper = lineCoverageMapper;
        }

        public ICoverageEngine CreateCoverageEngine()
        {
            return new BlanketJsCoverageEngine(jsonSerializer, fileSystem, lineCoverageMapper, urlBuilder);
        }
    }
}
