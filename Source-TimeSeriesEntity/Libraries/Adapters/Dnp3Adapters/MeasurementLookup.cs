﻿//******************************************************************************************************
//  MeasurementLookup.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/05/2012 - Adam Crain
//       Generated original version of source code.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using DNP3.Interface;
using GSF.TimeSeries;

namespace Dnp3Adapters
{
    /// <summary>
    /// Helper class that converts measurements and provides a lookup capbility
    /// </summary>
    class MeasurementLookup
    {
        public MeasurementLookup(MeasurementMap map)
        {
            map.binaryMap.ForEach(m => binaryMap.Add(m.dnpIndex, m));
            map.analogMap.ForEach(m => analogMap.Add(m.dnpIndex, m));
            map.counterMap.ForEach(m => counterMap.Add(m.dnpIndex, m));
            map.controlStatusMap.ForEach(m => controlStatusMap.Add(m.dnpIndex, m));
            map.setpointStatusMap.ForEach(m => setpointStatusMap.Add(m.dnpIndex, m));
        }

        public IMeasurement LookupMaybeNull(Binary meas, UInt32 index)
        {
            return LookupMaybeNull(meas, index, binaryMap, ConvertBinary);
        }

        public IMeasurement LookupMaybeNull(Analog meas, UInt32 index)
        {
            return LookupMaybeNull(meas, index, analogMap, ConvertAnalog);
        }

        public IMeasurement LookupMaybeNull(Counter meas, UInt32 index)
        {
            return LookupMaybeNull(meas, index, counterMap, ConvertCounter);
        }

        public IMeasurement LookupMaybeNull(ControlStatus meas, UInt32 index)
        {
            return LookupMaybeNull(meas, index, controlStatusMap, ConvertControlStatus);
        }

        public IMeasurement LookupMaybeNull(SetpointStatus meas, UInt32 index)
        {
            return LookupMaybeNull(meas, index, setpointStatusMap, ConvertSetpointStatus);
        }

        private Measurement ConvertBinary(Binary meas, uint id, String source)
        {
            var m = new Measurement();
            m.Key = new MeasurementKey(Guid.Empty, id, source);
            m.Value = meas.value ? 1.0 : 0.0;
            m.Timestamp = DateTime.UtcNow;
            return m;
        }

        private Measurement ConvertAnalog(Analog meas, uint id, String source)
        {
            var m = new Measurement();
            m.Key = new MeasurementKey(Guid.Empty, id, source);
            m.Value = meas.value;
            m.Timestamp = DateTime.UtcNow;
            return m;
        }

        private Measurement ConvertCounter(Counter meas, uint id, String source)
        {
            var m = new Measurement();
            m.Key = new MeasurementKey(Guid.Empty, id, source);
            m.Value = meas.value; //auto cast to double
            m.Timestamp = DateTime.UtcNow;
            return m;
        }

        private Measurement ConvertControlStatus(ControlStatus meas, uint id, String source)
        {
            var m = new Measurement();
            m.Key = new MeasurementKey(Guid.Empty, id, source);
            m.Value = meas.value ? 1.0 : 0.0;
            m.Timestamp = DateTime.UtcNow;
            return m;
        }

        private Measurement ConvertSetpointStatus(SetpointStatus meas, uint id, String source)
        {
            var m = new Measurement();
            m.Key = new MeasurementKey(Guid.Empty, id, source);
            m.Value = meas.value;
            m.Timestamp = DateTime.UtcNow;
            return m;
        }

        private static IMeasurement LookupMaybeNull<T>(T meas, UInt32 index, Dictionary<UInt32, Mapping> map, Func<T, uint, String, Measurement> converter)
        {
            Mapping id;
            if (map.TryGetValue(index, out id)) return converter(meas, id.tsfId, id.tsfSource);
            else return null;
        }

        private readonly Dictionary<UInt32, Mapping> binaryMap = new Dictionary<uint, Mapping>();
        private readonly Dictionary<UInt32, Mapping> analogMap = new Dictionary<uint, Mapping>();
        private readonly Dictionary<UInt32, Mapping> counterMap = new Dictionary<uint, Mapping>();
        private readonly Dictionary<UInt32, Mapping> controlStatusMap = new Dictionary<uint, Mapping>();
        private readonly Dictionary<UInt32, Mapping> setpointStatusMap = new Dictionary<uint, Mapping>();
    }
}