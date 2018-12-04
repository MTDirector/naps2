﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using NAPS2.Images.Storage;
using NAPS2.Images.Transforms;
using NAPS2.Lang.Resources;
using NAPS2.Operation;
using NAPS2.Util;

namespace NAPS2.Images
{
    public class DeskewOperation : OperationBase
    {
        private readonly ImageRenderer imageRenderer;

        public DeskewOperation() : this(new ImageRenderer())
        {
        }

        public DeskewOperation(ImageRenderer imageRenderer)
        {
            this.imageRenderer = imageRenderer;

            AllowCancel = true;
            AllowBackground = true;
        }

        public bool Start(ICollection<ScannedImage> images)
        {
            ProgressTitle = MiscResources.AutoDeskewProgress;
            Status = new OperationStatus
            {
                StatusText = MiscResources.AutoDeskewing,
                MaxProgress = images.Count
            };

            RunAsync(() =>
            {
                var memoryLimitingSem = new Semaphore(4, 4);
                Pipeline.For(images).StepParallel(img =>
                {
                    if (CancelToken.IsCancellationRequested)
                    {
                        return null;
                    }
                    memoryLimitingSem.WaitOne();
                    var bitmap = imageRenderer.Render(img).Result;
                    try
                    {
                        if (CancelToken.IsCancellationRequested)
                        {
                            return null;
                        }
                        var transform = RotationTransform.Auto(bitmap);
                        if (CancelToken.IsCancellationRequested)
                        {
                            return null;
                        }
                        bitmap = Transform.Perform(bitmap, transform);
                        var thumbnail = Transform.Perform(bitmap, new ThumbnailTransform());
                        lock (img)
                        {
                            img.AddTransform(transform);
                            img.SetThumbnail(thumbnail);
                        }

                        // The final pipeline step is pretty fast, so updating progress here is more accurate
                        lock (this)
                        {
                            Status.CurrentProgress += 1;
                        }
                        InvokeStatusChanged();

                        return Tuple.Create(img, transform);
                    }
                    finally
                    {
                        bitmap.Dispose();
                        memoryLimitingSem.Release();
                    }
                }).Run();
                return !CancelToken.IsCancellationRequested;
            });

            return true;
        }
    }
}
