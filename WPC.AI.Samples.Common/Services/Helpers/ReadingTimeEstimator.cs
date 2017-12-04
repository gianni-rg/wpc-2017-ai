//
// Copyright (c) Gianni Rosa Gallina. All rights reserved.
// Licensed under the MIT license.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace WPC.AI.Samples.Common.Services.Helpers
{
    using System;
    using System.Collections.Generic;

    public static class ReadingTimeEstimator
    {
        // See: https://en.wikipedia.org/wiki/Words_per_minute
        //      http://iovs.arvojournals.org/article.aspx?articleid=2166061
        private const int m_meanWPM = 184; // words per minute +/- 29
        private static readonly Dictionary<string, int> m_WPM;

        static ReadingTimeEstimator()
        {
            m_WPM = new Dictionary<string, int>();

            m_WPM.Add("Arab", 138);       // +/- 20
            m_WPM.Add("Chinese", 158);    // +/- 19
            m_WPM.Add("Dutch", 202);      // +/- 29
            m_WPM.Add("English", 228);    // +/- 30
            m_WPM.Add("Finnish", 161);    // +/- 18
            m_WPM.Add("French", 195);     // +/- 26
            m_WPM.Add("German", 179);     // +/- 17
            m_WPM.Add("Hebrew", 187);     // +/- 29
            m_WPM.Add("Italian", 188);    // +/- 28
            m_WPM.Add("Japanase", 193);   // +/- 30
            m_WPM.Add("Polish", 166);     // +/- 23
            m_WPM.Add("Portuguese", 181); // +/- 29
            m_WPM.Add("Russian", 184);    // +/- 32
            m_WPM.Add("Slovenian", 180);  // +/- 30
            m_WPM.Add("Spanish", 218);    // +/- 28
            m_WPM.Add("Swedish", 199);    // +/- 34
            m_WPM.Add("Turkish", 166);    // +/- 25
        }

        public static double GetEstimatedReadingTime(int wordCount, string language = null)
        {
            double meanWPM = m_meanWPM;
            if (m_WPM.ContainsKey(language))
            {
                meanWPM = m_WPM[language];
            }

            return (double)wordCount / meanWPM;
        }

        public static double GetEstimatedReadingTime(string text, string language = null)
        {
            int wordCount = TextTokenizer.GetWordCount(text);
            return GetEstimatedReadingTime(wordCount, language);
        }
    }
}
