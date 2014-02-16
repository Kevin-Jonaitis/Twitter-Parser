If you want to quickly parse public twitter feeds for keywords, this is for you. A really hacked together, but quick to implement twitter Parser. In no way is this safe or organized, but it gets the job done. 

Disclaimer: Much of this code was copied off of StackOverflow/Blogs



To create a new parser, you need the four following things:
OAuthToken
OAuthTokenSecret
OAuthConsumerKey
OAuthConsumerSecret

These will be created when a create a twitter app. 

Once you have them, simply write these two lines, passing the keys in the order listed above:
            
parser = new Parser(key1, key2, key3, key4);
parser.findKeywords(keyword);

Tweets can be fetched from the Queue in the Parser class. They will contain name, location, and text variables.