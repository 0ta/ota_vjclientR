using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace ota.ndi
{
    internal sealed class RecordingTimeManager
    {
        private readonly int _targetFrameRate;

        private double _start;

        private double _last;

        public RecordingTimeManager(int targetFrameRate = 60)
        {
            _targetFrameRate = targetFrameRate;
        }

        public void Clear()
        {
            _start = 0;
        }

        public unsafe double getTime(ReadOnlySpan<byte> metadata)
        {
            var time = Time.unscaledTimeAsDouble - _start;

            if (_start == 0)
            {
                _start = Time.unscaledTimeAsDouble;
                _last = 0;
                return 0;
            }
            else
            {
                _last = time;
                return time;
            }
        }

        public bool isSameFrame(double time)
        {
            return (int)(time * _targetFrameRate) == (int)(_last * _targetFrameRate);
        }
    }
}
