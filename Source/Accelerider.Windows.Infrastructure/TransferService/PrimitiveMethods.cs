﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Accelerider.Windows.Infrastructure.TransferService
{
    public static class PrimitiveMethods
    {
        public static HttpWebRequest ToRequest(this string remotePath)
        {
            return WebRequest.CreateHttp(remotePath);
        }

        public static HttpWebRequest Slice(this HttpWebRequest request, (long offset, long length) block)
        {
            request.AddRangeBasedOffsetLength(block.offset, block.length);
            return request;
        }

        public static async Task<HttpWebResponse> GetResponseAsync(HttpWebRequest request)
        {
            return (HttpWebResponse)await request.GetResponseAsync();
        }

        public static FileStream ToStream(this string localPath)
        {
            return File.Open(localPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write); ;
        }

        public static FileStream Slice(this FileStream stream, (long offset, long length) block)
        {
            stream.Position = block.offset;
            return stream;
        }

        public static IObservable<BlockTransferContext> CreateBlockDownloadItem(Func<Task<(HttpWebResponse response, Stream inputStream)>> streamPairFactory, BlockTransferContext context) => Observable.Create<BlockTransferContext>(o =>
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            // Execute copy stream by async.
            Task.Run(async () =>
            {
                try
                {
                    (HttpWebResponse response, Stream inputStream) = await streamPairFactory();

                    using (response)
                    using (var outputStream = response.GetResponseStream())
                    using (inputStream)
                    {
                        byte[] buffer = new byte[128 * 1024];
                        int count = 0;
                        context.Bytes = count;
                        while ((count = await outputStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await inputStream.WriteAsync(buffer, 0, count, cancellationToken);
                            context.Bytes = count;
                            o.OnNext(context);
                        }
                    }

                    context.Bytes = 0;
                    o.OnNext(context);
                    o.OnCompleted();
                }
                catch (Exception e)
                {
                    o.OnError(new BlockTransferException(context, e));
                }
            }, cancellationToken);

            return () =>
            {
                Debug.WriteLine($"{context.Id} has been disposed. ");
                cancellationTokenSource.Cancel();
            };
        });

        public static IObservable<BlockTransferContext> Catch<TException>(
            this IObservable<BlockTransferContext> @this,
            Func<TException, IObservable<BlockTransferContext>> handler)
            where TException : Exception
        {
            return @this.Catch<BlockTransferContext, TException>(handler);
        }
    }
}
