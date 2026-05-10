using Avalonia.Metadata;
using Microtone.Interfaces.Score;
using Microtone.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace Microtone.Models
{
    public class Dimensions<T> : ICloneable
    {
        private protected List<T> _items;
        public IReadOnlyList<T> Items => _items;
        public T? Default = default;
        public int Count => _items.Count;
        // 1-indexedアクセス（1次元, 2次元, ...）
        public T? this[int dimension]
        {
            get
            {
                int index = dimension - 1;
                if (index < 0 || index >= _items.Count)
                    throw new ArgumentOutOfRangeException(nameof(dimension));
                return _items[index];
            }
            set
            {
                int index = dimension - 1;
                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(dimension), "次元は1以上である必要があります");

                // 必要に応じて拡張
                while (_items.Count <= index)
                    _items.Add(Default!);

                _items[index] = value!;
            }
        }

        public int MaxDimension
        {
            get
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    if (!EqualityComparer<T>.Default.Equals(_items[i], Default))
                        return i + 1;
                }
                return 0;
            }
        }

        public Dimensions() : this([], default) { }
        public Dimensions(IEnumerable<T> items) : this(items, default) { }
        public Dimensions(IEnumerable<T> items,T? _Default)
        {
            _items = [.. items];
            Default = _Default;
            if (_items.Count == 0) this[1] = default;
        }

        public object Clone() => new Dimensions<T>([.._items]);

    }

    public struct Ratio : IRatio
    {
        /// <summary>
        /// 分子
        /// </summary>
        public int Numerator { get; set; }
        /// <summary>
        /// 分母
        /// </summary>
        public int Denominator { get; set; }
        public readonly double Value => (double)Numerator / Denominator;
        public Ratio(int numerator, int denominator) { 
            Numerator = numerator;
            Denominator = denominator;
        }
    }
    public readonly struct RatioResult : IRatio
    {
        public int Numerator { get; init; }
        public int Denominator { get; init; }
        public readonly double Value => (double)Numerator / Denominator;
        public int MaxDimension { get; init; }
        public Ratio ToRatio => new() { Numerator = Numerator, Denominator = Denominator };
    }
    public class OvertoneFormula : Dimensions<int>
    {
        public void Add(int dimension, int value)
        {
            if (dimension < 1) throw new ArgumentOutOfRangeException(nameof(dimension), "次元は1以上である必要があります");
            if (_items.Count < dimension)
            {
                this[dimension] = value;
            }
            else
            {
                this[dimension] = this[dimension] + value;
            }
        }

        public double RatioValue
        {
            get
            {
                int a = 1, b = 1;
                for (var i = 0; i < _items.Count; i++)
                {
                    if (_items[i] == 0)
                    {
                        continue;
                    }else if (_items[i] > 0)
                    {
                        a *= (int)Math.Pow(App.Primes[i], _items[i]);
                    }
                    else
                    {
                        b *= (int)Math.Pow(App.Primes[i], -_items[i]);
                    }
                }
                return (double)a / b;
            }
        }

        public RatioResult Ratio
        {
            get
            {
                int a = 1, b = 1;
                for (var i = 0; i < _items.Count; i++)
                {
                    if (_items[i] == 0)
                    {
                        continue;
                    }
                    else if (_items[i] > 0)
                    {
                        a *= (int)Math.Pow(App.Primes[i], _items[i]);
                    }
                    else
                    {
                        b *= (int)Math.Pow(App.Primes[i], -_items[i]);
                    }
                }
                var n = GCD(a, b);
                return new RatioResult()
                {
                    Numerator = a / n,
                    Denominator = b / n,
                    MaxDimension = this.MaxDimension
                };
            }
        }

        public OvertoneFormula(IEnumerable<int> Dimensions)
        {
            _items = [.. Dimensions];
            Default = 0;
            if (_items.Count == 0) this[1] = 0;
        }

        public static OvertoneFormula operator +(OvertoneFormula a, OvertoneFormula b)
        {
            int maxDim = Math.Max(a.Count, b.Count);
            int minDim = Math.Min(a.Count, b.Count);
            var result = new OvertoneFormula([]);
            for (int i = 1; i <= minDim; i++)
            {
                result[i] = (short)(a[i] + b[i]);
            }
            for (int i = minDim + 1; i <= maxDim; i++)
            {
                if (a.Count > b.Count)
                {
                    result[i] = a[i];
                }
                else
                {
                    result[i] = b[i];
                }
            }
            return result;
        }
        public static OvertoneFormula operator -(OvertoneFormula a, OvertoneFormula b)
        {
            int maxDim = Math.Max(a.Count, b.Count);
            int minDim = Math.Min(a.Count, b.Count);
            var result = new OvertoneFormula([]);
            for (int i = 1; i <= minDim; i++)
            {
                result[i] = (short)(a[i] - b[i]);
            }
            for (int i = minDim + 1; i <= maxDim; i++)
            {
                if (a.Count > b.Count)
                {
                    result[i] = a[i];
                }
                else
                {
                    result[i] = (short)-b[i];
                }
            }
            return result;
        }
        public static OvertoneFormula operator *(OvertoneFormula a, int b)
        {
            var result = new OvertoneFormula([]);
            for (int i = 1; i <= a.Count; i++)
            {
                result[i] = (short)(a[i] * b);
            }
            return result;
        }
        private static int GCD(int x, int y)
        {
            while (y != 0)
            {
                int temp = y;
                y = x % y;
                x = temp;
            }
            return x;
        }

        public new OvertoneFormula Clone() => new OvertoneFormula([.._items]);
    }

    public class Harmonograph : Dimensions<int>
    {
        public void Add(int dimension, int value)
        {
            if (dimension < 1) throw new ArgumentOutOfRangeException(nameof(dimension), "次元は1以上である必要があります");
            if (_items.Count < dimension)
            {
                this[dimension] = value;
            }
            else
            {
                this[dimension] = this[dimension] + value;
            }
        }
        public Harmonograph(IEnumerable<int> Dimensions)
        {
            _items = [.. Dimensions];
            Default = 0;
            if (_items.Count == 0) this[1] = 0;
        }
        public Harmonograph(OvertoneFormula formula, Dimensions<int> offset1d)
        {
            var f = formula.Clone();
            for (int i = f.MaxDimension; i >= 2; i--)
            {
                if (i > offset1d.Count) throw new ArgumentException("定義が足りません");
                this[i] = f[i];
                // i次元 f[i]ステップ分の1次元補正を除去
                f[1] -= f[i] * offset1d[i];
                f[i] = 0;
            }
            this[1] = f[1];
        }
        public OvertoneFormula ToOvertoneFormula(Dimensions<int> offset1d)
        {
            var result = new OvertoneFormula([]);
            int dim1 = this[1];
            for (int i = 2; i <= Count; i++)
            {
                if (i > offset1d.Count) throw new ArgumentException("定義が足りません");
                result[i] = this[i];
                dim1 += (this[i]) * (offset1d[i]);
            }
            result[1] = dim1;
            return result;
        }
        public bool IsSingleStep
        {
            get
            {
                bool flag = false;
                for (int i = 1; i <= Count; i++)
                {
                    if (Math.Abs(this[i]) > 1)
                        return false;
                    if (Math.Abs(this[i]) == 1)
                    {
                        if (flag) return false;
                        else flag = true;
                    }
                        
                }
                return flag;
            }
        }

        public static Harmonograph operator +(Harmonograph a, Harmonograph b)
        {
            int maxDim = Math.Max(a.Count, b.Count);
            int minDim = Math.Min(a.Count, b.Count);
            var result = new Harmonograph([]);
            for (int i = 1; i <= minDim; i++)
            {
                result[i] = (short)(a[i] + b[i]);
            }
            for (int i = minDim + 1; i <= maxDim; i++)
            {
                if (a.Count > b.Count)
                {
                    result[i] = a[i];
                }
                else
                {
                    result[i] = b[i];
                }
            }
            return result;
        }
        public static Harmonograph operator -(Harmonograph a, Harmonograph b)
        {
            int maxDim = Math.Max(a.Count, b.Count);
            int minDim = Math.Min(a.Count, b.Count);
            var result = new Harmonograph([]);
            for (int i = 1; i <= minDim; i++)
            {
                result[i] = (short)(a[i] - b[i]);
            }
            for (int i = minDim + 1; i <= maxDim; i++)
            {
                if (a.Count > b.Count)
                {
                    result[i] = a[i];
                }
                else
                {
                    result[i] = (short)-b[i];
                }
            }
            return result;
        }
        public static Harmonograph operator *(Harmonograph a, int b)
        {
            var result = new Harmonograph([]);
            for (int i = 1; i <= a.Count; i++)
            {
                result[i] = (short)(a[i] * b);
            }
            return result;
        }

        public new Harmonograph Clone() => new Harmonograph([.._items]);
    }
}
