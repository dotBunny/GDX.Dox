// Copyright (c) 2020-2022 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

namespace Dox.Utils;

public class Html
{
    public static HtmlObject BuildObject(string text)
    {
        HtmlObject returnObject = new HtmlObject();

        string header = GetTagContent("head", text);
        if (header != null)
        {
            returnObject.Style = GetTagContent("style", header);
        }

        returnObject.Body = GetTagContent("body", text);

        return returnObject;
    }

    public class HtmlObject
    {
        public string Style;
        public string Body;
    }


    public static string GetTagContent(string tag, string wholeContent)
    {
        // Quick check if we actually have the tag in question
        int tagCheckIndex = wholeContent.IndexOf($"<{tag}", 0);
        if (tagCheckIndex == -1) return null;

        // Find the actual content portion
        int tagStartIndex = wholeContent.IndexOf(">", tagCheckIndex);
        int tagEndIndex = -1;
        if (tagStartIndex != -1)
        {
            tagEndIndex = wholeContent.IndexOf($"</{tag}>", tagStartIndex);
        }

        if (tagStartIndex != -1 && tagEndIndex != -1)
        {
            return wholeContent.Substring(tagStartIndex + 1, tagEndIndex - tagStartIndex - 1);
        }
        return null;
    }
}