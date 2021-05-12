// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.JSInterop.Implementation
{
    /// <summary>
    /// Implements functionality for <see cref="IJSObjectReference"/>.
    /// </summary>
    // Note that the same concrete implementation can represent either an object or a data reference. Developers
    // work in terms of the interfaces which indicate the set of method it's useful to call.
    public class JSObjectReference : IJSObjectReference, IJSDataReference
    {
        private readonly JSRuntime _jsRuntime;

        // If we wanted, we could have a separate JSDataReference class rather than merging the concepts into JSObjectReference.
        // However it might require a bunch more duplication in the logic that transports and converts things. Would need investigation.

        internal bool Disposed { get; set; }

        /// <summary>
        /// The unique identifier assigned to this instance.
        /// </summary>
        protected internal long Id { get; }

        /// <summary>
        /// Inititializes a new <see cref="JSObjectReference"/> instance.
        /// </summary>
        /// <param name="jsRuntime">The <see cref="JSRuntime"/> used for invoking JS interop calls.</param>
        /// <param name="id">The unique identifier.</param>
        protected internal JSObjectReference(JSRuntime jsRuntime, long id)
        {
            _jsRuntime = jsRuntime;

            Id = id;
        }

        /// <inheritdoc />
        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, object?[]? args)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeAsync<TValue>(Id, identifier, args);
        }

        /// <inheritdoc />
        public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            ThrowIfDisposed();

            return _jsRuntime.InvokeAsync<TValue>(Id, identifier, cancellationToken, args);
        }

        System.IO.Stream IJSDataReference.OpenReadStream(long maxLength, CancellationToken cancellationToken)
            => _jsRuntime.ReadJSDataAsStream(this, maxLength, cancellationToken);

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (!Disposed)
            {
                Disposed = true;

                await _jsRuntime.InvokeVoidAsync("DotNet.jsCallDispatcher.disposeJSObjectReferenceById", Id);
            }
        }

        /// <inheritdoc />
        protected void ThrowIfDisposed()
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
