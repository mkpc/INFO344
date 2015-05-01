using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Collections;

namespace PA2
{
    public class TrieNode
    {
        public char keyChar { get; set; }
        public bool isWord;
        public HybridDictionary child { get; set; }

        public TrieNode() { }

        public TrieNode(char key)
        {
            this.keyChar = key;
        }
        public TrieNode(char key, bool isWord)
        {
            this.keyChar = key;
            this.isWord = isWord;
        }


        public bool ContainsKey(char key)
        {
            try
            {
                return child.Contains(key);
            }
             catch(NullReferenceException)
            {
                return false;
            }
        }

        public TrieNode FindKey(char key)
        {
            return (TrieNode)child[key];
        }

        internal TrieNode AddTitleHelper(char keyChar, bool isWord)
        {
            if (child == null)
                child = new HybridDictionary();

            if (!child.Contains(keyChar))
            {
                if (isWord)
                {

                    child.Add(keyChar, null);
                    return null;
                }
                else
                {
                    TrieNode node = new TrieNode(keyChar, isWord);
                    child.Add(keyChar, node);
                    return node;
                }
            }
            return (TrieNode)child[keyChar];
        }
    }
}