using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace PA2
{
    public class Trie
    {
       
        TrieNode root = new TrieNode(' ', false);

        //insert title into trie
        public void AddTitle(string title)
        {
            title = title.ToLower() + "#";
            TrieNode current = root;
            char[] arr = title.ToCharArray();
            int a = arr.Length;
            for (int i = 0; i < a; i++)
            {
               char temp = arr[i];
               if (temp != '#')
               {              
                   current = current.AddTitleHelper(temp, false);
               }
               else
               {
                   current.isWord = true;
               }
            }
        }

        //pass in “hello” => returns “hello, hello me, hello world” in test set
        public List<string> SearchForPrefix(string input)
        {
            input = input.Replace(" ", "_");
            List<string> result = new List<string>();
            int wordlength = input.Length;
            SearchHelper(result, input, wordlength, 0, root, "");
            return result;
            
        }

        private void SearchHelper(List<string> result, string input, int wordlength, int current, TrieNode root, String word)
        {
            if (root == null || result.Count == 100)
            {
                return;
            }
            else
            {


                if (root.keyChar == '_')
                {
                    root.keyChar = ' ';
                }



                word = word + root.keyChar;



                if (root.isWord)
                {
                    if (word.Length > input.Length)
                    {
                        result.Add(word);
                    }


                }

                if (current < wordlength)//follow the user input
                {

                    if (root.ContainsKey(input[current]))
                    {
                        TrieNode node = root.FindKey(input[current]);
                        SearchHelper(result, input, wordlength, current + 1, node, word);
                    }
                }
                else
                {
                    if (root.child != null)//has next node after this
                    {
                        Char[] myKeys = new Char[root.child.Count];
                        root.child.Keys.CopyTo(myKeys, 0);
                        for (int i = 0; i < root.child.Count; i++)
                        {
                            TrieNode node = root.FindKey(myKeys[i]);
                            SearchHelper(result, input, wordlength, current + 1, node, word);
                        }

                    }
                }
            }
        }
    }
}