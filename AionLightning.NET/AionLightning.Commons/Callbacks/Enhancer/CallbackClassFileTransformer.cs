using System;
using Microsoft.Extensions.Logging;


namespace AionLightning.Commons.Callbacks.Enhancer
{
    /// <summary>
    /// Placeholder for CallbackClassFileTransformer.
    /// In .NET, this would be part of an AOP framework's pipeline.
    /// </summary>
    public abstract class CallbackClassFileTransformer
    {
        private readonly ILogger<CallbackClassFileTransformer> _log;
        private readonly GlobalCallbackEnhancer _globalCallbackEnhancer;
        private readonly ObjectCallbackEnhancer _objectCallbackEnhancer;

        public CallbackClassFileTransformer(ILogger<CallbackClassFileTransformer> log, GlobalCallbackEnhancer globalCallbackEnhancer, ObjectCallbackEnhancer objectCallbackEnhancer)
        {
            _log = log;
            _globalCallbackEnhancer = globalCallbackEnhancer;
            _objectCallbackEnhancer = objectCallbackEnhancer;
        }

        public byte[] Transform(string className, byte[] classFileBuffer)
        {
            try
            {
                // In .NET we don't need to worry about class loaders in the same way.
                // We might want to filter out system libraries though.
                if (className.StartsWith("System.") || className.StartsWith("Microsoft."))
                {
                    _log.LogTrace($"Class {className} ignored.");
                    return null;
                }

                return TransformClass(classFileBuffer);
            }
            catch (Exception e)
            {
                _log.LogError(e, $"Can't transform class {className}");
                // The original code had a halt here. We'll throw an exception instead.
                throw new InvalidOperationException($"Can't transform class {className}", e);
            }
        }

        public abstract byte[] TransformClass(byte[] classBytes);
    }
}