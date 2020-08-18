using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

/// <summary>
/// Application to filter Twitter achive into a slimmer JSON with crud filtered out
/// Licenced under MIT license (see EOF comment)
/// Written by O. Westin http://microsff.com https://twitter.com/MicroSFF
/// </summary>
namespace FilterTwitterJson
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                // Define parameters
                ArgParser parser = new ArgParser(true);
                parser.AddDefinition(new string[] { "-i", "--input" }, true, 1, 1, "Input archive file");
                parser.AddDefinition(new string[] { "-o", "--output" }, true, 1, 1, "Output JSON file");
                parser.AddDefinition(new string[] { "-u", "--user" }, false, 1, 1, "User ID to keep replies for");
                parser.AddDefinition(new string[] { "-f", "--from" }, false, 1, 1, "Start date (YYYY-MM-DD)");
                parser.AddDefinition(new string[] { "-t", "--to" }, false, 1, 1, "End date (YYYY-MM-DD)");
                parser.AddDefinition(new string[] { "-c", "--contains" }, false, 1, -1, "Filter out tweets containing one of given strings");
                parser.AddDefinition(new string[] { "-b", "--begins" }, false, 1, -1, "Filter out tweets beginning with one of given strings");
                parser.AddDefinition(new string[] { "-r", "--retweets" }, false, 0, 0, "Filter out retweets");
                parser.AddDefinition(new string[] { "-e", "--edits" }, false, 0, 0, "Filter out edits");
                parser.AddDefinition(new string[] { "-d", "--display" }, false, 0, 0, "Display edits (requires -e)");
                parser.AddDefinition(new string[] { "-p", "--pretty" }, false, 0, 0, "Pretty-print output");
                parser.AddDefinition(new string[] { "-h", "--help" }, false, 0, 0, "Show help", true);

                // Parse and validate arguments
                Dictionary<string, List<string>> arguments = parser.ValidateArgs(args);

                // If help was requested, display it and end program
                if (arguments.ContainsKey("-h"))
                {
                    Console.WriteLine("FilterTwitterJson arguments:");
                    Console.WriteLine(parser);
                    return 0;
                }

                // Add filters
                JsonFilter filter = new JsonFilter();

                if (arguments.ContainsKey("-r"))
                    filter.AddFilter(new RetweetFilter());

                if (arguments.ContainsKey("-b"))
                    filter.AddFilter(new StartsWithFilter(arguments["-b"]));

                if (arguments.ContainsKey("-u"))
                    filter.AddFilter(new ReplyFilter(arguments["-u"][0]));

                if (arguments.ContainsKey("-c"))                                   
                    filter.AddFilter(new ContainsFilter(arguments["-c"]));

                if (arguments.ContainsKey("-f") || arguments.ContainsKey("-t"))
                {
                    // Support some flexibility in 1-digit month and day number
                    string[] formats = { "yyyy-M-d", "yyyy-M-dd", "yyyy-MM-d", "yyyy-MM-dd" };
                    DateTime start = default, end = default;
                    if (arguments.ContainsKey("-f"))
                    {
                        foreach (var format in formats)
                        {
                            if (DateTime.TryParseExact(arguments["-f"][0], format, null, DateTimeStyles.None, out start))
                                break;
                        }
                        if (start == default)
                            throw new Exception(string.Format("Failed to parse date: {0}", arguments["-f"][0]));
                    }
                    if (arguments.ContainsKey("-t"))
                    {
                        foreach (var format in formats)
                        {
                            if (DateTime.TryParseExact(arguments["-f"][0], format, null, DateTimeStyles.None, out start))
                                break;
                        }
                        if (start == default)
                            throw new Exception(string.Format("Failed to parse date: {0}", arguments["-t"][0]));
                    }
                    filter.AddFilter(new DateFilter(start, end));
                }

                string infile = arguments["-i"][0];
                string outfile = arguments["-o"][0];
                string ext = Path.GetExtension(infile);

                // Read tweets from zip or js file
                List<JsonTweet> tweets;
                if (ext == ".zip")
                    tweets = JsonReader.ReadArchiveFile(infile);
                else if (ext == ".js")
                    tweets = JsonReader.ReadJsonFile(infile);
                else
                    throw new Exception(string.Format("Unknown extension (.zip and .js supported): {0}", infile));

                Console.WriteLine(string.Format("{0} tweets read from: {1}", tweets.Count, Path.GetFileName(infile)));

                // Apply filters
                bool excludeCorrections = arguments.ContainsKey("-c");
                List<Tuple<JsonTweet, JsonTweet>> corrections = new List<Tuple<JsonTweet, JsonTweet>>();
                List<JsonTweet> filteredTweets = filter.FilterTweets(tweets, excludeCorrections, corrections);

                // Write result
                var options = new JsonSerializerOptions
                {
                    WriteIndented = arguments.ContainsKey("-p"),
                };
                string jsonString = JsonSerializer.Serialize(filteredTweets, options);
                File.WriteAllText(outfile, jsonString);

                Console.WriteLine(string.Format("{0} tweets written to: {1}", filteredTweets.Count, Path.GetFileName(outfile)));
                Console.WriteLine(filter.ReportFilterStatistics());

                if (excludeCorrections && arguments.ContainsKey("-d"))
                {
                    foreach (var pair in corrections)
                    {
                        Console.WriteLine(string.Format("{0}\t{1}", pair.Item1.created_at_time, pair.Item1.full_text));
                        Console.WriteLine(string.Format("{0}\t{1}", pair.Item2.created_at_time, pair.Item2.full_text));
                        Console.WriteLine();
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("Error: {0}", e.Message));
                return -1;
            }
        }
    }
}
/*
Copyright 2020 O. Westin 

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in 
the Software without restriction, including without limitation the rights to 
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

Except as contained in this notice, the name(s) of the above copyright holders
shall not be used in advertising or otherwise to promote the sale, use or
other dealings in this Software without prior written authorization.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
THE SOFTWARE.
*/
