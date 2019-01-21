using System;
using System.Collections.Generic;
using System.IO;

namespace CCGCurator.ReferenceBuilder
{
    class TemporaryFileManager : IDisposable
    {
        private readonly List<string> paths = new List<string>();

        public string GetTemporaryFileName()
        {
            var result = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
            paths.Add(result);
            return result;
        }

        public string GetTemporaryFileName(string extension)
        {
            var result = Path.Combine(Path.GetTempPath(), Path.GetTempFileName() + extension);
            paths.Add(result);
            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (var path in paths)
                    TryDelete(path);

                disposedValue = true;
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        ~TemporaryFileManager()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
