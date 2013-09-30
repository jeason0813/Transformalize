#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System.Collections.Generic;
using System.Linq;
using Transformalize.Libs.Ninject.Components;
using Transformalize.Libs.Ninject.Infrastructure;
using Transformalize.Libs.Ninject.Infrastructure.Disposal;
using Transformalize.Libs.Ninject.Planning.Bindings;

namespace Transformalize.Libs.Ninject.Activation.Caching
{
    /// <summary>
    ///     Tracks instances for re-use in certain scopes.
    /// </summary>
    public class Cache : NinjectComponent, ICache
    {
        /// <summary>
        ///     Contains all cached instances.
        ///     This is a dictionary of scopes to a multimap for bindings to cache entries.
        /// </summary>
        private readonly IDictionary<object, Multimap<IBindingConfiguration, CacheEntry>> entries =
            new Dictionary<object, Multimap<IBindingConfiguration, CacheEntry>>(new WeakReferenceEqualityComparer());

        /// <summary>
        ///     Initializes a new instance of the <see cref="Cache" /> class.
        /// </summary>
        /// <param name="pipeline">The pipeline component.</param>
        /// <param name="cachePruner">The cache pruner component.</param>
        public Cache(IPipeline pipeline, ICachePruner cachePruner)
        {
            Ensure.ArgumentNotNull(pipeline, "pipeline");
            Ensure.ArgumentNotNull(cachePruner, "cachePruner");

            Pipeline = pipeline;
            cachePruner.Start(this);
        }

        /// <summary>
        ///     Gets the pipeline component.
        /// </summary>
        public IPipeline Pipeline { get; private set; }

        /// <summary>
        ///     Gets the number of entries currently stored in the cache.
        /// </summary>
        public int Count
        {
            get { return GetAllCacheEntries().Count(); }
        }

        /// <summary>
        ///     Stores the specified context in the cache.
        /// </summary>
        /// <param name="context">The context to store.</param>
        /// <param name="reference">The instance reference.</param>
        public void Remember(IContext context, InstanceReference reference)
        {
            Ensure.ArgumentNotNull(context, "context");

            var scope = context.GetScope();
            var entry = new CacheEntry(context, reference);

            lock (entries)
            {
                var weakScopeReference = new ReferenceEqualWeakReference(scope);
                if (!entries.ContainsKey(weakScopeReference))
                {
                    entries[weakScopeReference] = new Multimap<IBindingConfiguration, CacheEntry>();
                    var notifyScope = scope as INotifyWhenDisposed;
                    if (notifyScope != null)
                    {
                        notifyScope.Disposed += (o, e) => Clear(weakScopeReference);
                    }
                }

                entries[weakScopeReference].Add(context.Binding.BindingConfiguration, entry);
            }
        }

        /// <summary>
        ///     Tries to retrieve an instance to re-use in the specified context.
        /// </summary>
        /// <param name="context">The context that is being activated.</param>
        /// <returns>
        ///     The instance for re-use, or <see langword="null" /> if none has been stored.
        /// </returns>
        public object TryGet(IContext context)
        {
            Ensure.ArgumentNotNull(context, "context");
            var scope = context.GetScope();
            if (scope == null)
            {
                return null;
            }

            lock (entries)
            {
                Multimap<IBindingConfiguration, CacheEntry> bindings;
                if (!entries.TryGetValue(scope, out bindings))
                {
                    return null;
                }

                foreach (var entry in bindings[context.Binding.BindingConfiguration])
                {
                    if (context.HasInferredGenericArguments)
                    {
                        var cachedArguments = entry.Context.GenericArguments;
                        var arguments = context.GenericArguments;

                        if (!cachedArguments.SequenceEqual(arguments))
                        {
                            continue;
                        }
                    }

                    return entry.Reference.Instance;
                }

                return null;
            }
        }

        /// <summary>
        ///     Deactivates and releases the specified instance from the cache.
        /// </summary>
        /// <param name="instance">The instance to release.</param>
        /// <returns>
        ///     <see langword="True" /> if the instance was found and released; otherwise <see langword="false" />.
        /// </returns>
        public bool Release(object instance)
        {
            lock (entries)
            {
                var instanceFound = false;
                foreach (var bindingEntry in entries.Values.SelectMany(bindingEntries => bindingEntries.Values).ToList())
                {
                    var instanceEntries = bindingEntry.Where(cacheEntry => ReferenceEquals(instance, cacheEntry.Reference.Instance)).ToList();
                    foreach (var cacheEntry in instanceEntries)
                    {
                        Forget(cacheEntry);
                        bindingEntry.Remove(cacheEntry);
                        instanceFound = true;
                    }
                }

                return instanceFound;
            }
        }

        /// <summary>
        ///     Removes instances from the cache which should no longer be re-used.
        /// </summary>
        public void Prune()
        {
            lock (entries)
            {
                var disposedScopes = entries.Where(scope => !((ReferenceEqualWeakReference) scope.Key).IsAlive).Select(scope => scope).ToList();
                foreach (var disposedScope in disposedScopes)
                {
                    Forget(GetAllBindingEntries(disposedScope.Value));
                    entries.Remove(disposedScope.Key);
                }
            }
        }

        /// <summary>
        ///     Immediately deactivates and removes all instances in the cache that are owned by
        ///     the specified scope.
        /// </summary>
        /// <param name="scope">The scope whose instances should be deactivated.</param>
        public void Clear(object scope)
        {
            lock (entries)
            {
                Multimap<IBindingConfiguration, CacheEntry> bindings;
                if (entries.TryGetValue(scope, out bindings))
                {
                    Forget(GetAllBindingEntries(bindings));
                    entries.Remove(scope);
                }
            }
        }

        /// <summary>
        ///     Immediately deactivates and removes all instances in the cache, regardless of scope.
        /// </summary>
        public void Clear()
        {
            lock (entries)
            {
                Forget(GetAllCacheEntries());
                entries.Clear();
            }
        }

        /// <summary>
        ///     Releases resources held by the object.
        /// </summary>
        /// <param name="disposing"></param>
        public override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                Clear();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Gets all entries for a binding withing the selected scope.
        /// </summary>
        /// <param name="bindings">The bindings.</param>
        /// <returns>All bindings of a binding.</returns>
        private static IEnumerable<CacheEntry> GetAllBindingEntries(IEnumerable<KeyValuePair<IBindingConfiguration, ICollection<CacheEntry>>> bindings)
        {
            return bindings.SelectMany(bindingEntries => bindingEntries.Value);
        }

        /// <summary>
        ///     Gets all cache entries.
        /// </summary>
        /// <returns>Returns all cache entries.</returns>
        private IEnumerable<CacheEntry> GetAllCacheEntries()
        {
            return entries.SelectMany(scopeCache => GetAllBindingEntries(scopeCache.Value));
        }

        /// <summary>
        ///     Forgets the specified cache entries.
        /// </summary>
        /// <param name="cacheEntries">The cache entries.</param>
        private void Forget(IEnumerable<CacheEntry> cacheEntries)
        {
            foreach (var entry in cacheEntries.ToList())
            {
                Forget(entry);
            }
        }

        /// <summary>
        ///     Forgets the specified entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        private void Forget(CacheEntry entry)
        {
            Clear(entry.Reference.Instance);
            Pipeline.Deactivate(entry.Context, entry.Reference);
        }

        /// <summary>
        ///     An entry in the cache.
        /// </summary>
        private class CacheEntry
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="CacheEntry" /> class.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="reference">The instance reference.</param>
            public CacheEntry(IContext context, InstanceReference reference)
            {
                Context = context;
                Reference = reference;
            }

            /// <summary>
            ///     Gets the context of the instance.
            /// </summary>
            /// <value>The context.</value>
            public IContext Context { get; private set; }

            /// <summary>
            ///     Gets the instance reference.
            /// </summary>
            /// <value>The instance reference.</value>
            public InstanceReference Reference { get; private set; }
        }
    }
}