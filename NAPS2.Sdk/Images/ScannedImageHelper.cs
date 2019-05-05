﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using NAPS2.Config;
using NAPS2.Images.Storage;
using NAPS2.Images.Transforms;
using NAPS2.Ocr;
using NAPS2.Operation;
using NAPS2.Scan;
using NAPS2.Scan.Experimental;
using NAPS2.Util;

namespace NAPS2.Images
{
    public class ScannedImageHelper
    {
        public static string SaveSmallestBitmap(Bitmap sourceImage, ScanBitDepth bitDepth, bool highQuality, int quality, out ImageFormat imageFormat)
        {
            // Store the image in as little space as possible
            if (sourceImage.PixelFormat == PixelFormat.Format1bppIndexed)
            {
                // Already encoded as 1-bit
                imageFormat = ImageFormat.Png;
                return EncodePng(sourceImage);
            }
            else if (bitDepth == ScanBitDepth.BlackWhite)
            {
                // Convert to a 1-bit bitmap before saving to help compression
                // This is lossless and takes up minimal storage (best of both worlds), so highQuality is irrelevant
                using (var bitmap = BitmapHelper.CopyToBpp(sourceImage, 1))
                {
                    imageFormat = ImageFormat.Png;
                    return EncodePng(bitmap);
                }
                // Note that if a black and white image comes from native WIA, bitDepth is unknown,
                // so the image will be png-encoded below instead of using a 1-bit bitmap
            }
            else if (highQuality)
            {
                // Store as PNG
                // Lossless, but some images (color/grayscale) take up lots of storage
                imageFormat = ImageFormat.Png;
                return EncodePng(sourceImage);
            }
            else if (Equals(sourceImage.RawFormat, ImageFormat.Jpeg))
            {
                // Store as JPEG
                // Since the image was originally in JPEG format, PNG is unlikely to have size benefits
                imageFormat = ImageFormat.Jpeg;
                return EncodeJpeg(sourceImage, quality);
            }
            else
            {
                // Store as PNG/JPEG depending on which is smaller
                var pngEncoded = EncodePng(sourceImage);
                var jpegEncoded = EncodeJpeg(sourceImage, quality);
                if (new FileInfo(pngEncoded).Length <= new FileInfo(jpegEncoded).Length)
                {
                    // Probably a black and white image (from native WIA, so bitDepth is unknown), which PNG compresses well vs. JPEG
                    File.Delete(jpegEncoded);
                    imageFormat = ImageFormat.Png;
                    return pngEncoded;
                }
                else
                {
                    // Probably a color or grayscale image, which JPEG compresses well vs. PNG
                    File.Delete(pngEncoded);
                    imageFormat = ImageFormat.Jpeg;
                    return jpegEncoded;
                }
            }
        }

        private static string GetTempFilePath()
        {
            return Path.Combine(Paths.Temp, Path.GetRandomFileName());
        }

        private static string EncodePng(Bitmap bitmap)
        {
            var tempFilePath = GetTempFilePath();
            bitmap.Save(tempFilePath, ImageFormat.Png);
            return tempFilePath;
        }

        private static string EncodeJpeg(Bitmap bitmap, int quality)
        {
            var tempFilePath = GetTempFilePath();
            if (quality == -1)
            {
                bitmap.Save(tempFilePath, ImageFormat.Jpeg);
            }
            else
            {
                quality = Math.Max(Math.Min(quality, 100), 0);
                var encoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
                var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                bitmap.Save(tempFilePath, encoder, encoderParams);
            }
            return tempFilePath;
        }

        private readonly OperationProgress operationProgress;
        private readonly OcrRequestQueue ocrRequestQueue;
        private readonly BlankDetector blankDetector;

        public ScannedImageHelper()
        {
            operationProgress = OperationProgress.Default;
            ocrRequestQueue = OcrRequestQueue.Default;
            blankDetector = BlankDetector.Default;
        }

        public ScannedImageHelper(OperationProgress operationProgress, OcrRequestQueue ocrRequestQueue, BlankDetector blankDetector)
        {
            this.operationProgress = operationProgress;
            this.ocrRequestQueue = ocrRequestQueue;
            this.blankDetector = blankDetector;
        }

        public IImage PostProcessStep1(IImage output, ScanProfile profile, bool supportsNativeUI = true)
        {
            double scaleFactor = 1;
            if (!profile.UseNativeUI || !supportsNativeUI)
            {
                scaleFactor = 1.0 / profile.AfterScanScale.ToIntScaleFactor();
            }
            var result = Transform.Perform(output, new ScaleTransform(scaleFactor));

            if ((!profile.UseNativeUI || !supportsNativeUI) && (profile.ForcePageSize || profile.ForcePageSizeCrop))
            {
                float width = output.Width / output.HorizontalResolution;
                float height = output.Height / output.VerticalResolution;
                if (float.IsNaN(width) || float.IsNaN(height))
                {
                    width = output.Width;
                    height = output.Height;
                }
                PageDimensions pageDimensions = profile.PageSize.PageDimensions() ?? profile.CustomPageSize;
                if (pageDimensions.Width > pageDimensions.Height && width < height)
                {
                    if (profile.ForcePageSizeCrop)
                    {
                        result = Transform.Perform(result, new CropTransform(
                            0,
                            (int)((width - (float)pageDimensions.HeightInInches()) * output.HorizontalResolution),
                            0,
                            (int)((height - (float)pageDimensions.WidthInInches()) * output.VerticalResolution)
                        ));
                    }
                    else
                    {
                        result.SetResolution((float)(output.Width / pageDimensions.HeightInInches()),
                            (float)(output.Height / pageDimensions.WidthInInches()));
                    }
                }
                else
                {
                    if (profile.ForcePageSizeCrop)
                    {
                        result = Transform.Perform(result, new CropTransform
                        (
                            0,
                            (int)((width - (float)pageDimensions.WidthInInches()) * output.HorizontalResolution),
                            0,
                            (int)((height - (float)pageDimensions.HeightInInches()) * output.VerticalResolution)
                        ));
                    }
                    else
                    {
                        result.SetResolution((float)(output.Width / pageDimensions.WidthInInches()), (float)(output.Height / pageDimensions.HeightInInches()));
                    }
                }
            }

            return result;
        }

        public void PostProcessStep2(ScannedImage scannedImage, IImage image, ScanProfile profile, ScanParams scanParams, int pageNumber, bool supportsNativeUI = true)
        {
            if (scanParams.ThumbnailSize.HasValue)
            {
                scannedImage.SetThumbnail(Transform.Perform(image, new ThumbnailTransform(scanParams.ThumbnailSize.Value)));
            }
            if (scanParams.SkipPostProcessing)
            {
                return;
            }
            if ((!profile.UseNativeUI || !supportsNativeUI) && profile.BrightnessContrastAfterScan)
            {
                if (profile.Brightness != 0)
                {
                    AddTransformAndUpdateThumbnail(scannedImage, ref image, new BrightnessTransform(profile.Brightness), scanParams);
                }
                if (profile.Contrast != 0)
                {
                    AddTransformAndUpdateThumbnail(scannedImage, ref image, new TrueContrastTransform(profile.Contrast), scanParams);
                }
            }
            if (profile.FlipDuplexedPages && pageNumber % 2 == 0)
            {
                AddTransformAndUpdateThumbnail(scannedImage, ref image, new RotationTransform(180), scanParams);
            }
            if (profile.AutoDeskew)
            {
                var op = new DeskewOperation();
                if (op.Start(new[] { scannedImage }, new DeskewParams { ThumbnailSize = scanParams.ThumbnailSize }))
                {
                    operationProgress.ShowProgress(op);
                    op.Wait();
                }
            }
            if (scanParams.DetectPatchCodes && scannedImage.PatchCode == PatchCode.None)
            {
                scannedImage.PatchCode = PatchCodeDetector.Detect(image);
            }
        }

        public bool ShouldDoBackgroundOcr(ScanParams scanParams)
        {
            if (ocrRequestQueue == null)
            {
                return false;
            }
            return scanParams.DoOcr && !string.IsNullOrEmpty(scanParams.OcrParams?.LanguageCode);
        }

        public string SaveForBackgroundOcr(IImage bitmap, ScanParams scanParams)
        {
            if (ShouldDoBackgroundOcr(scanParams))
            {
                var fileStorage = StorageManager.Convert<FileStorage>(bitmap, new StorageConvertParams { Temporary = true });
                // TODO: Maybe return the storage rather than the path
                return fileStorage.FullPath;
            }
            return null;
        }

        public void RunBackgroundOcr(ScannedImage image, ScanParams scanParams, string tempPath)
        {
            if (ShouldDoBackgroundOcr(scanParams))
            {
                using (var snapshot = image.Preserve())
                {
                    if (scanParams.DoOcr == true)
                    {
                        ocrRequestQueue.QueueForeground(null, snapshot, tempPath, scanParams.OcrParams, scanParams.OcrCancelToken).AssertNoAwait();
                    }
                    else
                    {
                        ocrRequestQueue.QueueBackground(snapshot, tempPath, scanParams.OcrParams);
                    }
                }
            }
        }

        private void AddTransformAndUpdateThumbnail(ScannedImage scannedImage, ref IImage image, Transform transform, ScanParams scanParams)
        {
            scannedImage.AddTransform(transform);
            if (scanParams.ThumbnailSize.HasValue)
            {
                var thumbnail = scannedImage.GetThumbnail();
                if (thumbnail != null)
                {
                    image = Transform.Perform(image, transform);
                    scannedImage.SetThumbnail(Transform.Perform(image, new ThumbnailTransform(scanParams.ThumbnailSize.Value)));
                }
            }
        }

        public ScannedImage PostProcess(IImage output, int pageNumber, ScanProfile scanProfile, ScanParams scanParams)
        {
            using (var result = PostProcessStep1(output, scanProfile))
            {
                if (blankDetector.ExcludePage(result, scanProfile))
                {
                    return null;
                }

                BitDepth bitDepth = scanProfile.UseNativeUI ? BitDepth.Color : scanProfile.BitDepth.ToBitDepth();
                var image = new ScannedImage(result, bitDepth, scanProfile.MaxQuality, scanProfile.Quality);
                PostProcessStep2(image, result, scanProfile, scanParams, pageNumber);
                string tempPath = SaveForBackgroundOcr(result, scanParams);
                RunBackgroundOcr(image, scanParams, tempPath);
                return image;
            }
        }
    }
}