using System.Collections.Generic;

namespace MinorShift.Emuera.Sub
{
    /// <summary>
    ///     字句解析結果の保存場所。Listとその現在位置を結びつけるためのもの。
    ///     基本的に全てpublicで
    /// </summary>
    internal sealed class WordCollection
    {
        private static readonly Word nullToken = new NullWord();
        public List<Word> Collection = new List<Word>();
        public int Pointer;

        public Word Current
        {
            get
            {
                if (Pointer >= Collection.Count)
                    return nullToken;
                return Collection[Pointer];
            }
        }

        public bool EOL => Pointer >= Collection.Count;

        public void Add(Word token)
        {
            Collection.Add(token);
        }

        public void Add(WordCollection wc)
        {
            Collection.AddRange(wc.Collection);
        }

        public void Clear()
        {
            Collection.Clear();
        }

        public void ShiftNext()
        {
            Pointer++;
        }

        public void Insert(Word w)
        {
            Collection.Insert(Pointer, w);
        }

        public void InsertRange(WordCollection wc)
        {
            Collection.InsertRange(Pointer, wc.Collection);
        }

        public void Remove()
        {
            Collection.RemoveAt(Pointer);
        }

        public void SetIsMacro()
        {
            foreach (var word in Collection)
                word.SetIsMacro();
        }

        public WordCollection Clone()
        {
            var ret = new WordCollection();
            for (var i = 0; i < Collection.Count; i++)
                ret.Collection.Add(Collection[i]);
            return ret;
        }

        public WordCollection Clone(int start, int count)
        {
            var ret = new WordCollection();
            if (start > Collection.Count)
                return ret;
            var end = start + count;
            if (end > Collection.Count)
                end = Collection.Count;
            for (var i = start; i < end; i++)
                ret.Collection.Add(Collection[i]);
            return ret;
        }
    }
}