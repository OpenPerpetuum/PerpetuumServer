using Autofac;
using Perpetuum.Bootstrapper;
using Perpetuum.Data;
using Perpetuum.EntityFramework;
using Perpetuum.Items;
using Perpetuum.Modules;
using Perpetuum.Robots;
using Perpetuum.Services.ExtensionService;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Perpetuum.DataDumper {

    public class PerpetuumLightBootstrapper : PerpetuumBootstrapper {
        // public void Init(string gameRoot)

        public DataDumper Dumper;

        public void InitDumper(string serverRoot, string dictionaryPath)
        {
            Dumper = new DataDumper(GetContainer(), serverRoot, dictionaryPath);
        }

    }
}
