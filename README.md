# INFO344

This is a web crawler project, that used to read and store all web pages from CNN.com to Mircosoft Azure storage. 

For the front-end part, I built a Trie for search function which provided auto-suggestion feature. The web allowed users to query the database by keywords, then display web pages and images.

For the back-end, I built a worker role to read "rebot.txt" and save all the discovered URLs to Azure storage. I also built a  Trie with 10,000 keywords from Wikipedia, that for the front-end used. 
