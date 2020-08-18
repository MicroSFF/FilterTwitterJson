using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Application to filter Twitter achive into a slimmer JSON with crud filtered out
/// Licenced under MIT license (see EOF comment)
/// Written by O. Westin http://microsff.com https://twitter.com/MicroSFF
/// </summary>
namespace FilterTwitterJson
{
    /// <summary>
    /// Interface of filters to apply to the tweets
    /// </summary>
    interface IFilter
    {
        /// <summary>
        /// Check tweet to determine whether to filter it out
        /// </summary>
        /// <param name="tweet">tweet to check</param>
        /// <returns>true if tweet should be filtered out</returns>
        bool Exclude(JsonTweet tweet);

        /// <summary>
        /// Number of tweets excluded
        /// </summary>
        uint Count
        { get; }
    }

    /// <summary>
    /// Filter out replies to others
    /// </summary>
    class ReplyFilter : IFilter
    {
        /// <summary>
        /// Filter out replies to others
        /// </summary>
        /// <param name="ownId">Id of user whose replies to keep</param>
        public ReplyFilter(string ownId)
        {
            this.ownId = ownId;
        }

        /// <summary>
        /// Check tweet to determine whether to filter it out
        /// </summary>
        /// <param name="tweet">tweet to check</param>
        /// <returns>true if tweet should be filtered out</returns>
        public bool Exclude(JsonTweet tweet)
        {
            if (!string.IsNullOrEmpty(tweet.in_reply_to_user_id) && (tweet.in_reply_to_user_id != ownId))
            {
                Count++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Number of tweets excluded
        /// </summary>
        public uint Count { get; private set; } = 0;

        /// <summary>
        /// Report number of exluded tweets
        /// </summary>
        /// <returns>String with filter type and number of exluded tweets</returns>
        public override string ToString()
        {
            return String.Format("Replies: {0}", Count);
        }

        private string ownId;
    }

    /// <summary>
    /// Filter out retweets
    /// </summary>
    class RetweetFilter : IFilter
    {
        /// <summary>
        /// Filter out retweets
        /// </summary>
        public RetweetFilter()
        { 
            // Could use generated default constructor, but wanted to have summary comment on it
        }        

        /// <summary>
        /// Check tweet to determine whether to filter it out
        /// </summary>
        /// <param name="tweet">tweet to check</param>
        /// <returns>true if tweet should be filtered out</returns>
        public bool Exclude(JsonTweet tweet)
        {
            // Checked with archives from 2019-04-14 and 2020-07-03 - the "retweeted" flag is always false, even for retweets
            if (tweet.retweeted || (tweet.full_text.IndexOf("RT ") == 0) || (tweet.full_text.IndexOf("MT ") == 0))
            {
                Count++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Number of tweets excluded
        /// </summary>
        public uint Count { get; private set; } = 0;

        /// <summary>
        /// Report number of exluded tweets
        /// </summary>
        /// <returns>String with filter type and number of exluded tweets</returns>
        public override string ToString()
        {
            return String.Format("Retweets: {0}", Count);
        }
    }

    /// <summary>
    /// Filter out tweets startig with one of given strings
    /// </summary>
    class StartsWithFilter : IFilter
    {
        /// <summary>
        /// Filter out tweets startig with one of given strings
        /// </summary>
        /// <param name="elements">strings to filter out on</param>
        public StartsWithFilter(List<string> elements)
        {
            this.elements = elements;
        }

        /// <summary>
        /// Check tweet to determine whether to filter it out
        /// </summary>
        /// <param name="tweet">tweet to check</param>
        /// <returns>true if tweet should be filtered out</returns>
        public bool Exclude(JsonTweet tweet)
        {
            foreach (var element in elements)
            {
                if (tweet.full_text.IndexOf(element) != -1)
                {
                    Count++;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Number of tweets excluded
        /// </summary>
        public uint Count { get; private set; } = 0;

        /// <summary>
        /// Report number of exluded tweets
        /// </summary>
        /// <returns>String with filter type and number of exluded tweets</returns>
        public override string ToString()
        {
            string tags = "";
            foreach (var element in elements)
            {
                tags += "\"" + element + "\", ";
            }
            if (!string.IsNullOrEmpty(tags))
            {
                tags = tags.Substring(0, tags.Length - 2);
            }
            return String.Format("Starts with {0}: {1}", tags, Count);
        }

        private List<string> elements;
    }

    /// <summary>
    /// Filter out unwanted substrings
    /// </summary>
    class ContainsFilter : IFilter
    {
        /// <summary>
        /// Filter out unwanted substrings
        /// </summary>
        /// <param name="tags">strings to filter out on</param>
        public ContainsFilter(List<string> elements)
        {
            this.elements = elements;
        }

        /// <summary>
        /// Check tweet to determine whether to filter it out
        /// </summary>
        /// <param name="tweet">tweet to check</param>
        /// <returns>true if tweet should be filtered out</returns>
        public bool Exclude(JsonTweet tweet)
        {
            foreach (var element in elements)
            {
                if (tweet.full_text.IndexOf(element) != -1)
                {
                    Count++;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Number of tweets excluded
        /// </summary>
        public uint Count { get; private set; } = 0;

        /// <summary>
        /// Report number of exluded tweets
        /// </summary>
        /// <returns>String with filter type and number of exluded tweets</returns>
        public override string ToString()
        {
            string tags = "";
            foreach (var element in elements)
            {
                tags += "\"" + element + "\", ";
            }
            if (!string.IsNullOrEmpty(tags))
            {
                tags = tags.Substring(0, tags.Length - 2);
            }
            return String.Format("Unwanted tags {0}: {1}", tags, Count);
        }

        private List<string> elements;
    }

    /// <summary>
    /// Filter out tweets outside given dates 
    /// </summary>
    class DateFilter : IFilter
    {
        /// <summary>
        /// Filter out tweets outside given dates 
        /// Dates can be default, in which case that limit is not applied
        /// </summary>
        /// <param name="start">start date</param>
        /// <param name="end">end date</param>
        public DateFilter(DateTime start, DateTime end)
        {
            hasStart = start != default(DateTime);
            hasEnd = end != default(DateTime);
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Check tweet to determine whether to filter it out
        /// </summary>
        /// <param name="tweet">tweet to check</param>
        /// <returns>true if tweet should be filtered out</returns>
        public bool Exclude(JsonTweet tweet)
        {
            if ((this.hasStart && (this.start > tweet.created_at_time)) || 
                (this.hasEnd && (this.end < tweet.created_at_time)))
            {
                Count++;
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Number of tweets excluded
        /// </summary>
        public uint Count { get; private set; } = 0;

        /// <summary>
        /// Report number of exluded tweets
        /// </summary>
        /// <returns>String with filter type and number of exluded tweets</returns>
        public override string ToString()
        {
            return String.Format("Outside time scope: {0}", Count);
        }

        private DateTime start;
        private DateTime end;
        private bool hasStart;
        private bool hasEnd;
    }

    /// <summary>
    /// Class to filter Json tweets out of a list
    /// </summary>
    class JsonFilter
    {
        /// <summary>
        /// Class to filter Json tweets out of a list
        /// </summary>
        public JsonFilter()
        {
            // Could use generated default constructor, but wanted to have summary comment on it
        }

        /// <summary>
        /// Add filter to the list of filters that will be applied
        /// </summary>
        /// <param name="filter">filter to add</param>
        public void AddFilter(IFilter filter)
        {
            filters.Add(filter);
        }

        /// <summary>
        /// Filter given list of tweets
        /// If corrections are excluded, that will be done after all filters have been applied
        /// </summary>
        /// <param name="tweets">tweets to filter</param>
        /// <param name="excludeCorrections">If true, exclude the first of any pair of consecutive tweets that are almost identical</param>
        /// <param name="corrections">If corrections are excluded, this will be filled with correction pairs (excluded, kept)</param>
        /// <returns>filtered list of tweets</returns>
        public List<JsonTweet> FilterTweets(List<JsonTweet> tweets, bool excludeCorrections, List<Tuple<JsonTweet, JsonTweet>> corrections)
        {
            List<JsonTweet> result = new List<JsonTweet>();
            correctionCount = 0;
            this.excludeCorrections = excludeCorrections;

            JsonTweet previous = null;
            TimeSpan correctionWindow = TimeSpan.FromHours(12);
            foreach (JsonTweet tweet in tweets)
            {
                // Apply filters
                bool exclude = false;
                foreach (IFilter filter in filters)
                {
                    if (exclude = filter.Exclude(tweet))
                        break;
                }
                if (exclude)
                    continue;

                // Compare to previous?
                if (excludeCorrections && (previous != null))
                { 
                    if ((tweet.created_at_time - previous.created_at_time) < correctionWindow)
                    {
                        int distance = LevenshteinDistance.Compute(tweet.full_text, previous.full_text);
                        if (distance <= levenshteinDistanceLimit)
                        {
                            // Correction, so remove previous from list
                            result.Remove(previous);
                            ++correctionCount;
                            corrections.Add(new Tuple<JsonTweet, JsonTweet>(previous, tweet));
                        }
                    }
                }
                previous = tweet;
                result.Add(tweet);
            }
            return result;
        }

        /// <summary>
        /// Report filter statistics
        /// </summary>
        /// <returns>Multiline string with each applied filter and number of exclusions</returns>
        public string ReportFilterStatistics()
        {
            StringBuilder sb = new StringBuilder();
            foreach (IFilter filter in filters)
            {
                sb.AppendLine(filter.ToString());
            }
            if (excludeCorrections)
                sb.AppendLine(string.Format("Corrections: {0}", correctionCount));
            return sb.ToString();
        }

        /// <summary>
        /// The Levenshtein distance limit used to identify corrections
        /// </summary>
        private int levenshteinDistanceLimit = 10;

        private List<IFilter> filters = new List<IFilter>();
        private uint correctionCount = 0;
        private bool excludeCorrections = false;
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
