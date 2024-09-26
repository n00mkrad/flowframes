using System;
using System.Collections.Generic;
using System.Linq;

namespace Flowframes.Utilities
{
    public class RollingAverage<T> where T : struct
    {
        public int CurrentSize { get => _values.Count; }
        public double Average { get => GetAverage(); }

        private Queue<T> _values;
        public Queue<T> Queue { get => _values; }
        private int _size;

        public RollingAverage(int size)
        {
            _values = new Queue<T>(size);
            _size = size;
        }

        public void AddDataPoint(T dataPoint)
        {
            if (_values.Count >= _size)
            {
                _values.Dequeue();
            }

            _values.Enqueue(dataPoint);
        }

        public double GetAverage()
        {
            // Convert the values to double before averaging, this is necessary because Average() does not work directly on generic types
            if(_values == null || _values.Count == 0)
            {
                return 0;
            }

            return _values.Select(val => Convert.ToDouble(val)).Average();
        }

        public double GetAverage(int lastXSamples)
        {
            if (lastXSamples <= 0)
            {
                lastXSamples = 1;
            }
            else if (lastXSamples > _values.Count)
            {
                lastXSamples = _values.Count;
            }

            // Take the last X samples and calculate the average
            return _values.Skip(Math.Max(0, _values.Count - lastXSamples)).Select(val => Convert.ToDouble(val)).Average();
        }

        public double GetAverage(float percentile)
        {
            int lastXSamples = (int)Math.Ceiling(_size * percentile);
            return GetAverage(lastXSamples);
        }

        public void Reset()
        {
            _values.Clear();
        }
    }
}
