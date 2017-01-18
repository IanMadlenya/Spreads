﻿using Spreads.Collections.Concurrent;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace Spreads {

    public abstract class ConvertSeries<TKey, TValue, TKey2, TValue2, TImpl> : Series<TKey2, TValue2>, IDisposable
        where TImpl : ConvertSeries<TKey, TValue, TKey2, TValue2, TImpl>, new() {
        private static readonly BoundedConcurrentBag<TImpl> Pool = new BoundedConcurrentBag<TImpl>(Environment.ProcessorCount * 2);

        protected ISeries<TKey, TValue> Inner;

        protected ConvertSeries(ISeries<TKey, TValue> inner) {
            Inner = inner;
            Comparer = new ConvertComparer(this as TImpl);
        }

        public ConvertSeries() {
        }

        public abstract TKey2 ToKey2(TKey key);

        public abstract TValue2 ToValue2(TValue value);

        public abstract TKey ToKey(TKey2 key2);

        public abstract TValue ToValue(TValue2 value2);

        public override IComparer<TKey2> Comparer { get; }

        public override ICursor<TKey2, TValue2> GetCursor() {
            return new ConvertCursor(Inner.GetCursor(), this as TImpl);
        }

        public static TImpl Create(ISeries<TKey, TValue> innerSeries) {
            TImpl inner;
            if (!Pool.TryTake(out inner)) {
                inner = new TImpl();
            }
            inner.Inner = innerSeries;
            return inner;
        }

        public virtual void Dispose(bool disposing) {
            var disposable = Inner as IDisposable;
            disposable?.Dispose();
            Inner = null;
            var pooled = Pool.TryAdd(this as TImpl);
            // TODO review
            if (disposing && !pooled) {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        ~ConvertSeries() {
            Dispose(false);
        }

        private struct ConvertCursor : ICursor<TKey2, TValue2> {
            private readonly ICursor<TKey, TValue> _innerCursor;
            private readonly TImpl _source;

            public ConvertCursor(ICursor<TKey, TValue> innerCursor, TImpl source) {
                _innerCursor = innerCursor;
                _source = source;
            }

            public Task<bool> MoveNext(CancellationToken cancellationToken) {
                return _innerCursor.MoveNext(cancellationToken);
            }

            public void Dispose() {
                _innerCursor.Dispose();
            }

            public bool MoveNext() {
                return _innerCursor.MoveNext();
            }

            public void Reset() {
                _innerCursor.Reset();
            }

            public KeyValuePair<TKey2, TValue2> Current
                => new KeyValuePair<TKey2, TValue2>(CurrentKey, CurrentValue);

            object IEnumerator.Current => Current;

            public bool MoveAt(TKey2 key, Lookup direction) {
                return _innerCursor.MoveAt(_source.ToKey(key), direction);
            }

            public bool MoveFirst() {
                return _innerCursor.MoveFirst();
            }

            public bool MoveLast() {
                return _innerCursor.MoveLast();
            }

            public bool MovePrevious() {
                return _innerCursor.MovePrevious();
            }

            public Task<bool> MoveNextBatch(CancellationToken cancellationToken) {
                return _innerCursor.MoveNextBatch(cancellationToken);
            }

            public ICursor<TKey2, TValue2> Clone() {
                return new ConvertCursor(_innerCursor.Clone(), _source);
            }

            public bool TryGetValue(TKey2 key, out TValue2 value) {
                TValue tmp;
                if (_innerCursor.TryGetValue(_source.ToKey(key), out tmp)) {
                    value = _source.ToValue2(tmp);
                    return true;
                }
                value = default(TValue2);
                return false;
            }

            public IComparer<TKey2> Comparer => _source.Comparer;
            public TKey2 CurrentKey => _source.ToKey2(_innerCursor.CurrentKey);
            public TValue2 CurrentValue => _source.ToValue2(_innerCursor.CurrentValue);

            // TODO object pooling
            public ISeries<TKey2, TValue2> CurrentBatch => Create(_innerCursor.CurrentBatch);

            public ISeries<TKey2, TValue2> Source => Create(_innerCursor.Source);
            public bool IsContinuous => _innerCursor.IsContinuous;
        }

        private class ConvertComparer : IComparer<TKey2> {
            private readonly TImpl _source;

            public ConvertComparer(TImpl source) {
                _source = source;
            }

            public int Compare(TKey2 x, TKey2 y) {
                var comparer = _source.Inner.Comparer;
                var x1 = _source.ToKey(x);
                var y1 = _source.ToKey(y);
                return comparer.Compare(x1, y1);
            }
        }
    }

    public abstract class ConvertMutableSeries<TKey, TValue, TKey2, TValue2, TImpl>
        : ConvertSeries<TKey, TValue, TKey2, TValue2, TImpl>, IMutableSeries<TKey2, TValue2>
        where TImpl : ConvertMutableSeries<TKey, TValue, TKey2, TValue2, TImpl>, new() {
        private static readonly BoundedConcurrentBag<TImpl> Pool = new BoundedConcurrentBag<TImpl>(Environment.ProcessorCount * 2);

        private IMutableSeries<TKey, TValue> MutableInner
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Unsafe.As<IMutableSeries<TKey, TValue>>(Inner); }
        }

        protected ConvertMutableSeries() {
        }

        protected ConvertMutableSeries(IMutableSeries<TKey, TValue> innerSeries) : base(innerSeries) {
        }

        public static TImpl Create(IMutableSeries<TKey, TValue> innerSeries) {
            TImpl inner;
            if (!Pool.TryTake(out inner)) {
                inner = new TImpl();
            }
            inner.Inner = innerSeries;
            return inner;
        }

        public override void Dispose(bool disposing) {
            var disposable = Inner as IDisposable;
            disposable?.Dispose();
            Inner = null;
            var pooled = Pool.TryAdd(this as TImpl);
            // TODO review
            if (disposing && !pooled) {
                GC.SuppressFinalize(this);
            }
        }

        ~ConvertMutableSeries() {
            Dispose(false);
        }

        public void Add(TKey2 key, TValue2 value) {
            MutableInner.Add(ToKey(key), ToValue(value));
        }

        public void AddLast(TKey2 key, TValue2 value) {
            MutableInner.AddLast(ToKey(key), ToValue(value));
        }

        public void AddFirst(TKey2 key, TValue2 value) {
            MutableInner.AddFirst(ToKey(key), ToValue(value));
        }

        public bool Remove(TKey2 key) {
            return MutableInner.Remove(ToKey(key));
        }

        public bool RemoveLast(out KeyValuePair<TKey2, TValue2> kvp) {
            throw new NotImplementedException();
        }

        public bool RemoveFirst(out KeyValuePair<TKey2, TValue2> kvp) {
            throw new NotImplementedException();
        }

        public bool RemoveMany(TKey2 key, Lookup direction) {
            throw new NotImplementedException();
        }

        public int Append(IReadOnlySeries<TKey2, TValue2> appendMap, AppendOption option) {
            throw new NotImplementedException();
        }

        public void Complete() {
            MutableInner.Complete();
        }

        public long Count => MutableInner.Count;

        public long Version
        {
            get { return MutableInner.Version; }
            set { MutableInner.Version = value; }
        }

        public override TValue2 this[TKey2 key] => ToValue2(MutableInner[ToKey(key)]);

        TValue2 IMutableSeries<TKey2, TValue2>.this[TKey2 key]
        {
            get { return ToValue2(MutableInner[ToKey(key)]); }
            set { MutableInner[ToKey(key)] = ToValue(value); }
        }
    }
}