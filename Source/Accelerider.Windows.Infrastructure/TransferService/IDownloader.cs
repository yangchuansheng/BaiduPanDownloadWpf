﻿using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Accelerider.Windows.Infrastructure.FileTransferService;

namespace Accelerider.Windows.Infrastructure.TransferService
{
    public interface IDownloader : IObservable<BlockTransferContext>, IDisposable, INotifyPropertyChanged
    {
        TransferStatus Status { get; }

        TransferContext Context { get; }

        Task ActivateAsync(CancellationToken cancellationToken = default);

        void Suspend();
    }
}
