# FilterTwitterJson #
Command-line application to read all tweets stored in JSON from a Twitter archive, filter them based on various criteria, and store in a new .js file.

Built on .NET Core 3.1
## Usage ##
`FilterTwitterJson -h/--help`<br>
Show help

`FilterTwitterJson -i [input file] -o [output file] [other arguments and their parameters]`<br>
Possible filters and flags:
<table>
<tr><th>Argument</th><th>Parameter(s)</th><th>Notes</th></tr>
<tr><td><code>-i</code>, <code>--input</code></td><td>1 - Input ZIP archive or JSON file</td><td>Mandatory</td></tr>
<tr><td><code>-o</code>, <code>--output</code></td><td>1 - Output JSON file</td><td>Mandatory</td></tr>
<tr><td><code>-u</code>, <code>--user</code></td><td>User ID to keep replies for</td><td>Replies to other users will be filtered out.</td></tr>
<tr><td><code>-f</code>, <code>--from</code></td><td>Start date (YYYY-MM-DD)</td><td>Filter out all tweets earlier than this date</td></tr>
<tr><td><code>-t</code>, <code>--to</code></td><td>End date (YYYY-MM-DD)</td><td>Filter out all tweets later than this date</td></tr>
<tr><td><code>-c</code>, <code>--contains</code></td><td>strings to filter on</td><td>Filter out all tweets containing one of given strings</td></tr>
<tr><td><code>-b</code>, <code>--begins</code></td><td>strings to filter on</td><td>Filter out tweets beginning with one of given strings</td></tr>
<tr><td><code>-r</code>, <code>--retweets</code></td><td>(none)</td><td>Filter out retweets</td><td></td></tr>
<tr><td><code>-e</code>, <code>--edits</code></td><td>(none)</td><td>Filter out edits</td><td>Edits are identified by comparing two consecutive tweets. If they are *almost* identical (e.g. [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) is < 10) the earlier one is filtered out</td></tr>
<tr><td><code>-d</code>, <code>--display</code></td><td>(none)</td><td>Display edits (requires -e) - date and text of the first (which is filtered out) and second</td><td></td></tr>
<tr><td><code>-p</code>, <code>--pretty</code></td><td>(none)</td><td>Pretty-print output</td><td></td></tr>
</table>

### Example ###
`-i tweets-2020-04-19.zip -o filtered.js -f 2019-5-04 -c dragon cat "space ship" -b "** " -r -e -p`<br>
This will read `tweets-2020-04-19.zip`, which presumably is the name of an archive downloaded from Twitter, and filter out:
* all tweets from before before May 4, 2019
* all tweets mentioning "dragon", "cat", or "space ship", even as part of a word (so tweets mentioning "category" will also be filtered out
* all tweets starting with "** "
* all retweets
* all edited tweets

The resulting list is written to `filtered.js` in a pretty-print format.

## Files ##
### ArgParser.cs ###
Contains `ArgParser`, a general class to structure and validate command-line arguments at a superficial level (checks number of parameters given with an argument, but not content).
### FilterTwitterJson.csproj ###
Project file.
### JsonFilter.cs ###
Contains `IFilter`, and interface for filters that can be applied to tweets, and a number of different filter classes implementing that interface: `ReplyFilter`, `RetweetFilter`, `StartsWithFilter`, `ContainsFilter`, and `DateFilter`.

Contains `JsonFilter`, a class which stores `IFilter` instances and applies them to a list of tweets. In addition to applying filters, it can also check the [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance) between two consecutive tweets to determine if the later is an edit of the former, and in that case discard the former.
### JsonReader.cs ###
Contains the classes `Entities`, `UserMention`, and `JsonTweet` which were all generated by [www.jsonutils.com](https://www.jsonutils.com/) based on examples from a Twitter archive. These classes represent the JSON data, and are what is serialised/deserialised.

Contains the class `JsonReader` which is a class with static functions, to read list of `JsonTweet` from either a ZIP archive or JSON file.
### LevenshteinDistance.cs ###
Contains the class `LevenshteinDistance`, with a static `Compute` function. This is an implementation of [Levenshtein distance](https://en.wikipedia.org/wiki/Levenshtein_distance), by [Marty Neal](https://stackoverflow.com/users/255244/marty-neal) on [StackOverflow](https://stackoverflow.com/questions/6944056/c-sharp-compare-string-similarity#6944095).
### Program.cs ###
Contains the `Program` class, and its static `Main` function.
## Licence ##
MIT licence
