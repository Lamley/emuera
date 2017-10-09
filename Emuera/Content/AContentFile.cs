using System;

namespace MinorShift.Emuera.Content
{
    internal abstract class AContentFile : IDisposable
    {
        public readonly string Filepath;
        public readonly string Name;
        protected bool Loaded = false;

        public AContentFile(string name, string path)
        {
            Name = name;
            Filepath = path;
        }

        public bool Enabled { get; protected set; }

        public abstract void Dispose();
    }
}