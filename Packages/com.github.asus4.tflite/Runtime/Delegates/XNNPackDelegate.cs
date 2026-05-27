/* Copyright 2018 The TensorFlow Authors. All Rights Reserved.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
==============================================================================*/

using System;
using System.Runtime.InteropServices;
using TfLiteDelegate = System.IntPtr;
using TfLiteXNNPackDelegateWeightsCache = System.IntPtr;

namespace TensorFlowLite
{
    public sealed class XNNPackDelegate : IDelegate
    {
        [System.Flags]
        public enum Flags : uint
        {
            // Enable XNNPACK acceleration for signed quantized 8-bit inference.
            // This includes operators with channel-wise quantized weights.
            QS8 = 0x00000001,
            // Enable XNNPACK acceleration for unsigned quantized 8-bit inference.
            QU8 = 0x00000002,
            // Force FP16 inference for FP32 operators.
            FORCE_FP16 = 0x00000004,
            // Enable XNNPACK acceleration for FULLY_CONNECTED operator with dynamic
            //weights.
            DYNAMIC_FULLY_CONNECTED = 0x00000008,
            // Enable XNNPACK acceleration for VAR_HANDLE, READ_VARIABLE, and
            // ASSIGN_VARIABLE operators.
            VARIABLE_OPERATORS = 0x00000010,
            // Enable transient indirection buffer to reduce memory usage in selected
            // operators. Indirection buffer initialization will take place on every
            // inference run, instead of only once during initialization of the operators.
            TRANSIENT_INDIRECTION_BUFFER = 0x00000020,
            // Enable the latest XNNPACK operators and features in the delegate which have
            // not yet been enabled by default.
            ENABLE_LATEST_OPERATORS = 0x00000040,
            // Enable XNNPack subgraph reshaping. This means that models with dynamic
            // tensors are supported and that inputs may be efficiently resized.
            ENABLE_SUBGRAPH_RESHAPING = 0x00000080,
            // This flag indicates that XNNPACK should attempt to produce numerically
            // consistent results from a specific build of XNNPACK. This causes XNNPACK
            // to avoid using faster codepaths that are numerically inconsistent with any
            // other codepath that could be used in the same compiled delegate.
            SLOW_CONSISTENT_ARITHMETIC = 0x00000200,
            // Disable XNNPack subgraph reshaping. This means that models with dynamic
            // tensors are not supported.
            DISABLE_SUBGRAPH_RESHAPING = 0x00000400,
            // Disable delegation of dynamically quantized ops.
            DISABLE_DYNAMICALLY_QUANTIZED_OPS = 0x00000800,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Options
        {
            // Number of threads to use in the thread pool.
            // 0 or negative value means no thread pool used.
            public int numThreads;
            public uint runtimeFlags;
            public Flags flags;
            // Cache for packed weights, can be shared between multiple instances of
            // delegates.
            public TfLiteXNNPackDelegateWeightsCache weightsCache;
            // Deprecated. Use the flags bitfield with the
            // TFLITE_XNNPACK_DELEGATE_FLAG_VARIABLE_OPERATORS mask.
            [Obsolete("Use the flags bitfield with the TFLITE_XNNPACK_DELEGATE_FLAG_VARIABLE_OPERATORS mask.")]
            public bool handleVariableOps;
            // Path to the weight cache to load.
            public IntPtr /* const char* */ weightCacheFilePath;
            // Explicit file descriptor for the weight cache.
            public int weightCacheFileDescriptor;
            // Points to an existing instance of a weight cache provider.
            public IntPtr /* void* */ weightCacheProvider;
        }

        public TfLiteDelegate Delegate { get; private set; }

        public static Options DefaultOptions => TfLiteXNNPackDelegateOptionsDefault();

        public static bool CanUseInMemoryWeightCacheProvider => TfLiteXNNPackDelegateCanUseInMemoryWeightCacheProvider();

        public static string InMemoryFilePath => Marshal.PtrToStringAnsi(TfLiteXNNPackDelegateInMemoryFilePath());

        public XNNPackDelegate() : this(DefaultOptions)
        {
        }

        public XNNPackDelegate(Options options)
        {
            UnityEngine.Debug.Log("XNNPackDelegate Created");
            Delegate = TfLiteXNNPackDelegateCreate(ref options);
        }

        public void Dispose()
        {
            TfLiteXNNPackDelegateDelete(Delegate);
            Delegate = TfLiteDelegate.Zero;
        }

        public static XNNPackDelegate DelegateForType(Type inputType)
        {
            Flags flags = 0;
            if (inputType == typeof(sbyte))
            {
                flags = Flags.QS8;
            }
            else if (inputType == typeof(byte))
            {
                flags = Flags.QU8;
            }
            var options = new Options()
            {
                numThreads = UnityEngine.SystemInfo.processorCount,
                flags = flags,
            };
            return new XNNPackDelegate(options);
        }

        #region Externs
        // APIs for XNNPack are included in the core library 
        internal const string TensorFlowLibrary = Interpreter.TensorFlowLibrary;

        // Returns true on systems that support running the in-memory weight cache
        // provider.
        [DllImport(TensorFlowLibrary)]
        private static extern bool TfLiteXNNPackDelegateCanUseInMemoryWeightCacheProvider();

        // Returns a file path that will activate the in-memory weight cache that
        // enables weight deduplication.
        [DllImport(TensorFlowLibrary)]
        private static extern unsafe IntPtr /* char* */ TfLiteXNNPackDelegateInMemoryFilePath();

        // Returns a structure with the default XNNPack delegate options.
        [DllImport(TensorFlowLibrary)]
        private static extern unsafe Options TfLiteXNNPackDelegateOptionsDefault();

        // Creates a new delegate instance that need to be destroyed with
        // `TfLiteXNNPackDelegateDelete` when delegate is no longer used by TFLite.
        // When `options` is set to `nullptr`, the following default values are used:
        [DllImport(TensorFlowLibrary)]
        private static extern unsafe TfLiteDelegate TfLiteXNNPackDelegateCreate(ref Options options);

        // Destroys a delegate created with `TfLiteXNNPackDelegateCreate` call.
        [DllImport(TensorFlowLibrary)]
        private static extern unsafe void TfLiteXNNPackDelegateDelete(TfLiteDelegate xnnPackDelegate);

        // Weights Cache is disable due to build error in iOS and Unity 2021 LTS.
        // https://github.com/asus4/tf-lite-unity-sample/issues/261

        // Creates a new weights cache that can be shared with multiple delegate instances.
        // [DllImport(TensorFlowLibrary)]
        // private static extern unsafe TfLiteXNNPackDelegateWeightsCache TfLiteXNNPackDelegateWeightsCacheCreate();        

        // Destroys a weights cache created with `TfLiteXNNPackDelegateWeightsCacheCreate` call.
        // [DllImport(TensorFlowLibrary)]
        // private static extern unsafe void TfLiteXNNPackWeightsCacheDelete(TfLiteXNNPackDelegateWeightsCache cache);
        #endregion // Externs
    }
}
