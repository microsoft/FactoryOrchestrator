// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Collections;
using System.Runtime.InteropServices;

namespace Microsoft.FactoryOrchestrator.Service
{
    [XmlRootAttribute(ElementName = "FOServiceStatus", IsNullable = false)]
    public sealed class FOServiceStatus
    {
        public FOServiceStatus()
        {
            FirstBootStateLoaded = false;
            FirstBootTaskListsComplete = false;
            LogFolder = "";
        }

        [XmlElement]
        public bool FirstBootStateLoaded { get; set; }

        [XmlElement]
        public bool FirstBootTaskListsComplete { get; set; }

        [XmlElement]
        public string LogFolder { get; set; }
    }

    [XmlRootAttribute(ElementName = "FOVolatileServiceStatus", IsNullable = false)]
    public sealed class FOVolatileServiceStatus
    {
        public FOVolatileServiceStatus()
        {
            EveryBootTaskListsComplete = false;
        }

        [XmlElement]
        public bool EveryBootTaskListsComplete { get; set; }
    }
}
