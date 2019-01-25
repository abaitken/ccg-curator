using System.IO;

namespace CCGCurator.ReferenceBuilder
{
    class Logging
    {
        private readonly StreamWriter writer;
        public Logging()
        {
            var logFileName = "collection.log";
            if (File.Exists(logFileName))
                File.Delete(logFileName);
            writer = new StreamWriter(logFileName);
        }

        public void Close()
        {
            writer.Flush();
            writer.Close();
        }

        public void WriteLine(string text)
        {
            writer.WriteLine(text);
        }
    }
}
